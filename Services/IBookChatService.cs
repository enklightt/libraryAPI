namespace LibraryAPI.Services
{
    public interface IBookChatService
    {
        Task<string?> AskAboutBookAsync(string bookId, string question, int? currentPage = null);
        Task AskAboutBookStreamAsync(string bookId, string question, Func<string, Task> onChunk, int? currentPage = null);
    }
}