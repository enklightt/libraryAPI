namespace LibraryAPI.Services;

public interface IBookTextService
{
    Task<(bool Found, string? Text, string? Title, string? Author)> GetBookTextAsync(string bookId);
}
