using Microsoft.EntityFrameworkCore;
using LibraryAPI.Models;

namespace LibraryAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ReadingProgress> ReadingProgress => Set<ReadingProgress>();
    public DbSet<WantToRead> WantToRead => Set<WantToRead>();
    public DbSet<DailyActivity> DailyActivities => Set<DailyActivity>();
    public DbSet<Friend> Friends => Set<Friend>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Favorite>()
            .HasKey(f => new { f.UserId, f.BookId });

        modelBuilder.Entity<WantToRead>()
            .HasKey(w => new { w.UserId, w.BookId });

        modelBuilder.Entity<Review>()
            .HasIndex(r => new { r.BookId, r.UserId })
            .IsUnique();

        modelBuilder.Entity<ReadingProgress>()
            .HasIndex(rp => new { rp.UserId, rp.BookId })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<Book>()
            .HasIndex(book => book.Isbn)
            .IsUnique();

        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
        modelBuilder.Entity<Genre>().ToTable("genres");
        modelBuilder.Entity<Book>().ToTable("books", table =>
        {
            table.HasCheckConstraint("CK_books_available_copies", "available_copies >= 0 AND available_copies <= total_copies");
            table.HasCheckConstraint("CK_books_total_copies", "total_copies BETWEEN 1 AND 1000");
            table.HasCheckConstraint("CK_books_pages", "pages IS NULL OR pages BETWEEN 1 AND 100000");
            table.HasCheckConstraint("CK_books_rating", "rating IS NULL OR rating BETWEEN 0 AND 5");
        });
        modelBuilder.Entity<Review>().ToTable("reviews");
        modelBuilder.Entity<Favorite>().ToTable("favorites");
        modelBuilder.Entity<ReadingProgress>().ToTable("reading_progress");
        modelBuilder.Entity<WantToRead>().ToTable("want_to_read");
        modelBuilder.Entity<DailyActivity>().ToTable("daily_activity");
        modelBuilder.Entity<Friend>(entity =>
        {
            entity.ToTable("friends");
            entity.HasOne(f => f.User)
                .WithMany(u => u.Friends)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.FriendUser)
                .WithMany()
                .HasForeignKey(f => f.FriendUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<FriendRequest>(entity =>
        {
            entity.ToTable("friend_requests");
            entity.HasOne(r => r.FromUser)
                .WithMany()
                .HasForeignKey(r => r.FromUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.ToUser)
                .WithMany()
                .HasForeignKey(r => r.ToUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}