namespace LibraryAPI.Interfaces;

public interface IBookTextService
{
    Task<(bool Found, string? Text, string? Title, string? Author)> GetBookTextAsync(string bookId);
}
