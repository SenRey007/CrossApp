using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class EventsExtraDemo
{
    public static void Run()
    {
        PartSinks();
        Console.WriteLine();
        PartImportAction();
    }

    private static void PartSinks()
    {
        Console.WriteLine("=== Додаткове 1: INotificationSink ===");
        string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, string.Empty);

        var sink = new CompositeSink(new ConsoleSink(), new FileSink(logPath));
        var service = new LendingService(new InMemoryBookCopyStore(), sink);

        int viaEvent = 0;
        service.Changed += (_, _) => viaEvent++;

        BookCopy copy = service.AddBook("978-966-03-1234-5");
        service.IssueCopy("L-700", copy.Id, "R-700", new DateOnly(2026, 10, 15));
        service.ReturnCopy("L-700", new DateOnly(2026, 10, 20));
        Console.WriteLine($" Подій через event: {viaEvent}");
        Console.WriteLine($" Рядків у sink.log: {File.ReadAllLines(logPath).Length}");

        DomainDemo.TryDo("повторне повернення",
            () => service.ReturnCopy("L-700", new DateOnly(2026, 10, 21)));
        Console.WriteLine($" Після відмови рядків у sink.log: {File.ReadAllLines(logPath).Length}");

        // Sink, який збирає повідомлення в список: так сервіс перевіряють у тесті, без консолі й файлу.
        var list = new ListSink();
        var testService = new LendingService(new InMemoryBookCopyStore(), list);
        testService.AddBook("978-617-12-5678-9");
        Console.WriteLine($" ListSink: {list.Kinds.Count} повідомлення, перше — {list.Kinds[0]}");
    }

    private static void PartImportAction()
    {
        Console.WriteLine("=== Додаткове 2: Action<string> в імпорті ===");
        string path = Path.Combine("data", "sample.csv");
        if (!File.Exists(path))
        {
            Console.WriteLine($" Файл не знайдено: {Path.GetFullPath(path)}");
            return;
        }

        int skipped = 0;
        ImportResult<BookDto> result = BookCsvImporter.Load(path, message =>
        {
            skipped++;
            Console.WriteLine($" пропущено: {message}");
        });
        Console.WriteLine($"Завантажено: {result.Items.Count}, повідомлень про пропуск: {skipped}");
    }

    private sealed class ListSink : INotificationSink
    {
        public List<LendingChangeKind> Kinds { get; } = [];
        public void Notify(LendingChangedEventArgs e) => Kinds.Add(e.Kind);
    }
}