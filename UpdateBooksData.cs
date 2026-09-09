using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;

namespace LibraryAPI;

public static class UpdateBooksData
{
    public static async Task UpdateAsync(AppDbContext context)
    {
        Console.WriteLine("\n📝 Оновлення даних книг...\n");

        var books = await context.Books.ToListAsync();

        foreach (var book in books)
        {
            switch (book.Title)
            {
                case "1984":
                    book.Quote = "Війна — це мир. Свобода — це рабство. Незнання — це сила.";
                    book.Pages = 328;
                    break;
                case "Animal Farm":
                    book.Quote = "Усі тварини рівні, але деякі тварини рівніші за інших.";
                    book.Pages = 112;
                    break;
                case "Кобзар":
                    book.Quote = "Борітеся — поборете, вам Бог помагає! За вас правда, за вас слава і воля святая!";
                    book.Pages = 256;
                    break;
                case "Pride and Prejudice":
                    book.Quote = "Це загальновизнана істина, що неодружений чоловік, який володіє статком, потребує дружини.";
                    book.Pages = 384;
                    break;
                case "The Prince":
                    book.Quote = "Краще, щоб тебе боялися, ніж любили, якщо не можеш досягти обох.";
                    book.Pages = 140;
                    break;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Оновлено {books.Count} книг з цитатами та кількістю сторінок\n");
    }
}
