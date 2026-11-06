using System.Net;
using System.Text.Json;

namespace LibraryAPI.Middleware
{
    /// <summary>
    /// Converts unhandled exceptions into safe JSON responses without exposing
    /// stack traces, connection strings or other internal implementation details.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Сталася необроблена помилка під час запиту {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);

                if (context.Response.HasStarted)
                    throw;

                var (statusCode, message) = ex switch
                {
                    KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                    ArgumentException => (HttpStatusCode.BadRequest, "Некоректний запит"),
                    UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Доступ заборонено"),
                    _ => (HttpStatusCode.InternalServerError, "Внутрішня помилка сервера")
                };

                context.Response.Clear();
                context.Response.StatusCode = (int)statusCode;
                context.Response.ContentType = "application/json; charset=utf-8";

                var response = new
                {
                    message,
                    traceId = context.TraceIdentifier
                };

                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json, context.RequestAborted);
            }
        }
    }
}
