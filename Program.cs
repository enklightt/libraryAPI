using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using DotNetEnv;
using LibraryAPI;
using LibraryAPI.Data;
using LibraryAPI.Infrastructure;
using LibraryAPI.Middleware;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is not configured. " +
            "Copy .env.example to .env and provide the database connection string.");
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 21)),
            mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )
        )
    );
}

var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (!isTesting)
    {
        throw new InvalidOperationException(
            "JwtSettings:Secret is not configured. " +
            "Copy .env.example to .env and provide a secret of at least 32 characters.");
    }

    jwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}

builder.Configuration["JwtSettings:Secret"] = jwtSecret;

if (jwtSecret.Length < 32)
    throw new InvalidOperationException("JwtSettings:Secret must be at least 32 characters long.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddMemoryCache(options => options.SizeLimit = 2048);
builder.Services.Configure<CachingOptions>(
    builder.Configuration.GetSection(CachingOptions.SectionName));
builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection(RateLimitingOptions.SectionName));
builder.Services.Configure<ResilienceOptions>(
    builder.Configuration.GetSection(ResilienceOptions.SectionName));
builder.Services.AddScoped<RequestCacheStatus>();

builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(x => new
            {
                field = x.Key,
                errors = x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            });

        return new BadRequestObjectResult(new
        {
            message = "Помилка валідації",
            errors
        });
    };
});

builder.Services.AddApiVersioning(options =>
    {
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Library API",
        Version = "v1",
        Description = "API для роботи з бібліотекою книг"
    });
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Library API",
        Version = "v2",
        Description = "Друга версія API з уніфікованою структурою відповіді"
    });
    c.DocInclusionPredicate((documentName, apiDescription) =>
        apiDescription.GroupName == documentName);

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length == 0)
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Cors:AllowedOrigins must be configured outside the Development environment.");
    }

    allowedOrigins = ["http://localhost:5043", "https://localhost:7043"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddResilientHttpClients(builder.Configuration);

builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IReadingProgressService, ReadingProgressService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWantToReadService, WantToReadService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IBookTextService, BookTextService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IBookChatService, BookChatService>();
builder.Services.AddScoped<IBookPdfService, BookPdfService>();
builder.Services.AddScoped<IGenreService, GenreService>();

var rateLimitingOptions = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();
var rateLimitingEnabled = rateLimitingOptions.Enabled && !isTesting;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Перевищено ліміт запитів. Спробуйте пізніше." },
            cancellationToken);
    };

    if (rateLimitingEnabled)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            if (HttpMethods.IsGet(context.Request.Method) ||
                HttpMethods.IsHead(context.Request.Method) ||
                HttpMethods.IsOptions(context.Request.Method))
            {
                return RateLimitPartition.GetNoLimiter("read");
            }

            var key = RateLimitKeyProvider.GetClientKey(context);
            return RateLimitPartition.GetFixedWindowLimiter(
                $"write:{key}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitingOptions.WritePermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
    }

    options.AddPolicy("auth", context =>
    {
        if (!rateLimitingEnabled)
            return RateLimitPartition.GetNoLimiter("auth-disabled");

        var key = RateLimitKeyProvider.GetClientKey(context);
        return RateLimitPartition.GetFixedWindowLimiter(
            $"auth:{key}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.AuthPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var app = builder.Build();

if (!isTesting)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseSeeder.SeedAsync(context);

    if (app.Environment.IsDevelopment())
    {
        await TestDataSeeder.SeedTestDataAsync(context);
        await UpdateBooksData.UpdateAsync(context);
    }
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Library API v1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Library API v2");
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();

public partial class Program { }
