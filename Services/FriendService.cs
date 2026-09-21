using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Interfaces;
using LibraryAPI.Models;

namespace LibraryAPI.Services;

public class FriendService : IFriendService
{
    private readonly AppDbContext _context;

    private static readonly Dictionary<string, string> AchievementTitles = new()
    {
        ["first_book"] = "Перший фініш",
        ["read_5_books"] = "Прочитав 5 книг",
        ["bookworm"] = "Книгоман",
        ["night_reader"] = "Нічний читач",
        ["favorite_collector"] = "Колекціонер",
    };

    public FriendService(AppDbContext context)
    {
        _context = context;
    }

    private static string? ResolveTitle(string? code)
    {
        return code != null && AchievementTitles.TryGetValue(code, out var title) ? title : null;
    }

    public async Task<IEnumerable<FriendDto>> GetFriendsAsync(string userId)
    {
        var friends = await _context.Friends
            .Include(f => f.FriendUser)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return friends.Select(f =>
        {
            var title = ResolveTitle(f.FriendUser.EquippedTitle);
            return new FriendDto
            {
                Id = f.Id,
                FriendUserId = f.FriendUserId,
                Name = f.FriendUser.Name,
                EquippedTitle = title,
                CreatedAt = f.CreatedAt
            };
        });
    }

    public async Task<(bool Success, string? Error)> SendFriendRequestAsync(string fromUserId, string toUserId)
    {
        if (string.IsNullOrWhiteSpace(toUserId))
            return (false, "Користувача не вказано");

        if (toUserId == fromUserId)
            return (false, "Не можна додати себе в друзі");

        var target = await _context.Users.FirstOrDefaultAsync(u => u.Id == toUserId);
        if (target == null)
            return (false, "Користувача не знайдено");

        var alreadyFriends = await _context.Friends.AnyAsync(f => f.UserId == fromUserId && f.FriendUserId == toUserId);
        if (alreadyFriends)
            return (false, "Ви вже друзі");

        var existingRequest = await _context.FriendRequests
            .FirstOrDefaultAsync(r => r.FromUserId == fromUserId && r.ToUserId == toUserId && r.Status == "pending");
        if (existingRequest != null)
            return (false, "Заявка вже відправлена");

        _context.FriendRequests.Add(new FriendRequest
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Status = "pending"
        });

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<IEnumerable<FriendRequestDto>> GetIncomingRequestsAsync(string userId)
    {
        var requests = await _context.FriendRequests
            .Include(r => r.FromUser)
            .Where(r => r.ToUserId == userId && r.Status == "pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(r => new FriendRequestDto
        {
            Id = r.Id,
            FromUserId = r.FromUserId,
            FromName = r.FromUser.Name,
            FromTitle = ResolveTitle(r.FromUser.EquippedTitle),
            Status = r.Status,
            CreatedAt = r.CreatedAt
        });
    }

    public async Task<IEnumerable<FriendRequestDto>> GetSentRequestsAsync(string userId)
    {
        var requests = await _context.FriendRequests
            .Include(r => r.ToUser)
            .Where(r => r.FromUserId == userId && r.Status == "pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(r => new FriendRequestDto
        {
            Id = r.Id,
            FromUserId = r.ToUserId,
            FromName = r.ToUser.Name,
            FromTitle = ResolveTitle(r.ToUser.EquippedTitle),
            Status = r.Status,
            CreatedAt = r.CreatedAt
        });
    }

    public async Task<(bool Success, string? Error)> AcceptFriendRequestAsync(string requestId, string userId)
    {
        var request = await _context.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ToUserId == userId && r.Status == "pending");

        if (request == null)
            return (false, "Заявку не знайдено");

        request.Status = "accepted";
        request.UpdatedAt = DateTime.UtcNow;

        _context.Friends.Add(new Friend { UserId = request.FromUserId, FriendUserId = request.ToUserId });
        _context.Friends.Add(new Friend { UserId = request.ToUserId, FriendUserId = request.FromUserId });

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> RejectFriendRequestAsync(string requestId, string userId)
    {
        var request = await _context.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ToUserId == userId && r.Status == "pending");

        if (request == null) return false;

        _context.FriendRequests.Remove(request);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelFriendRequestAsync(string requestId, string userId)
    {
        var request = await _context.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.FromUserId == userId && r.Status == "pending");

        if (request == null) return false;

        _context.FriendRequests.Remove(request);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveFriendAsync(string userId, string friendUserId)
    {
        var friend = await _context.Friends
            .FirstOrDefaultAsync(f => (f.UserId == userId && f.FriendUserId == friendUserId)
                                   || (f.UserId == friendUserId && f.FriendUserId == userId));

        if (friend == null) return false;

        var pair = await _context.Friends
            .Where(f => (f.UserId == userId && f.FriendUserId == friendUserId)
                     || (f.UserId == friendUserId && f.FriendUserId == userId))
            .ToListAsync();

        _context.Friends.RemoveRange(pair);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FriendProfileDto?> GetFriendProfileAsync(string friendUserId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == friendUserId);
        if (user == null) return null;

        var finished = await _context.ReadingProgress
            .CountAsync(rp => rp.UserId == friendUserId && rp.Status == "finished");
        var totalPages = await _context.ReadingProgress
            .Where(rp => rp.UserId == friendUserId)
            .SumAsync(rp => rp.CurrentPage);
        var readingNow = await _context.ReadingProgress
            .CountAsync(rp => rp.UserId == friendUserId && rp.Status == "reading" && rp.ProgressPercent < 100);

        return new FriendProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            EquippedTitle = ResolveTitle(user.EquippedTitle),
            FinishedBooks = finished,
            TotalPages = totalPages,
            ReadingNow = readingNow
        };
    }

    public async Task<IEnumerable<FavoriteBookDto>> GetFriendFavoritesAsync(string friendUserId)
    {
        var favorites = await _context.Favorites
            .Include(f => f.Book)
            .Where(f => f.UserId == friendUserId)
            .OrderByDescending(f => f.AddedAt)
            .ToListAsync();

        return favorites.Select(f => new FavoriteBookDto
        {
            Id = f.Book.Id,
            Title = f.Book.Title,
            Author = f.Book.Author,
            Isbn = f.Book.Isbn ?? "",
            GenreId = f.Book.GenreId,
            TotalCopies = 1,
            AvailableCopies = 1,
            PdfUrl = f.Book.PdfUrl,
            Rating = f.Book.Rating,
            IsActive = true,
            AddedAt = f.AddedAt
        });
    }

    public async Task<IEnumerable<ReadingProgressDto>> GetFriendReadingAsync(string friendUserId)
    {
        var progress = await _context.ReadingProgress
            .Include(rp => rp.Book)
            .Where(rp => rp.UserId == friendUserId && rp.Status == "reading" && rp.ProgressPercent < 100)
            .OrderByDescending(rp => rp.LastReadAt)
            .ToListAsync();

        return progress.Select(rp => new ReadingProgressDto
        {
            Id = rp.Id,
            UserId = rp.UserId,
            BookId = rp.BookId,
            BookTitle = rp.Book.Title,
            BookAuthor = rp.Book.Author,
            BookImageUrl = rp.Book.ImageUrl,
            BookIsbn = rp.Book.Isbn,
            BookRating = rp.Book.Rating,
            CurrentPage = rp.CurrentPage,
            TotalPages = rp.TotalPages,
            ProgressPercent = rp.ProgressPercent,
            LastReadAt = rp.LastReadAt,
            Status = rp.Status
        });
    }

    public async Task<IEnumerable<UserSearchResultDto>> SearchUsersAsync(string query, string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
            return Enumerable.Empty<UserSearchResultDto>();

        var friendIds = await _context.Friends
            .Where(f => f.UserId == currentUserId)
            .Select(f => f.FriendUserId)
            .ToListAsync();

        var requestIds = await _context.FriendRequests
            .Where(r => r.FromUserId == currentUserId && r.Status == "pending")
            .Select(r => r.ToUserId)
            .ToListAsync();

        var excludeIds = new List<string>(friendIds) { currentUserId };
        excludeIds.AddRange(requestIds);

        var users = await _context.Users
            .Where(u => u.Name.Contains(query)
                     && !excludeIds.Contains(u.Id)
                     && u.RoleId == 2)
            .OrderBy(u => u.Name)
            .Take(10)
            .ToListAsync();

        return users.Select(u => new UserSearchResultDto
        {
            Id = u.Id,
            Name = u.Name,
            EquippedTitle = ResolveTitle(u.EquippedTitle)
        });
    }
}
