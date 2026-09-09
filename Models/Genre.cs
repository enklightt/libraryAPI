namespace LibraryAPI.Models;

public class Genre
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = null!;

    public ICollection<Book> Books { get; set; } = [];
}
