using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class LendingDemo
{
    public static void Run(bool useFile)
    {
        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

        IBookCopyStore store = useFile
            ? new FileBookCopyStore(dataPath)
            : new InMemoryBookCopyStore(SampleData.Copies());

        var service = new LendingService(store);
        Console.WriteLine($"Сховище: {store.GetType().Name}");

        BookCopy created = service.AddBook("978-966-03-1234-5");
        Loan loan = service.IssueCopy("L-900", created.Id, "R-900", new DateOnly(2026, 10, 5));
        Console.WriteLine($"Видано: {loan}");

        Console.WriteLine("Каталог примірників:");
        foreach (BookCopy c in service.All())
            Console.WriteLine($" {c}");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій відмови ===");
        TryDo("дубль id примірника", () => store.Add(created));
        TryDo("видача з неіснуючим id примірника",
            () => service.IssueCopy("L-901", "NO-SUCH-ID", "R-901", new DateOnly(2026, 10, 5)));
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($" {title}: виняток НЕ спрацював!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
        }
    }
}