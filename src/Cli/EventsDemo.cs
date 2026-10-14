using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class EventsDemo
{
    public static void Run(string[] args)
    {
        string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, string.Empty); // чистий лог для кожного показу

        IBookCopyStore store = StoreFactory.Create(args);
        var service = new LendingService(store);
        Console.WriteLine($"Сховище: {store.GetType().Name}");

        // Підписник 1: локальний метод (його можна відписати через -=)
        void OnLendingChanged(object? sender, LendingChangedEventArgs e)
        {
            string text = e.Kind switch
            {
                LendingChangeKind.BookAdded => $"додано примірник {e.CopyId}",
                LendingChangeKind.CopyIssued =>
                    $"примірник {e.CopyId} видано читачу {e.ReaderId} до {e.DueDate:yyyy-MM-dd}",
                LendingChangeKind.CopyReturned =>
                    $"примірник {e.CopyId} повернено читачем {e.ReaderId}",
                _ => e.Kind.ToString()
            };

            string line = $"{e.At:HH:mm:ss} {e.Kind,-12} {text}";
            Console.WriteLine(line);
            File.AppendAllText(logPath, line + Environment.NewLine);
        }

        service.Changed += OnLendingChanged;

        // Підписник 2: лямбда із замиканням над локальною змінною
        int changes = 0;
        service.Changed += (_, _) => changes++;

        Console.WriteLine("=== Сценарій: події ===");
        BookCopy copy = service.AddBook("978-966-03-1234-5");
        service.IssueCopy("L-600", copy.Id, "R-600", new DateOnly(2026, 10, 15));
        service.ReturnCopy("L-600", new DateOnly(2026, 10, 20));
        Console.WriteLine($"Подій отримано: {changes}");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій відмови ===");
        DomainDemo.TryDo("видача неіснуючого примірника",
            () => service.IssueCopy("L-601", "NO-SUCH-ID", "R-601", new DateOnly(2026, 10, 21)));
        DomainDemo.TryDo("повернення неіснуючої видачі",
            () => service.ReturnCopy("L-999", new DateOnly(2026, 10, 21)));
        DomainDemo.TryDo("повторне повернення тієї ж видачі",
            () => service.ReturnCopy("L-600", new DateOnly(2026, 10, 21)));
        Console.WriteLine($"Подій отримано: {changes} (відмови подій не створили)");

        Console.WriteLine();
        Console.WriteLine("=== Після відписки ===");
        service.Changed -= OnLendingChanged;
        service.IssueCopy("L-602", copy.Id, "R-600", new DateOnly(2026, 10, 22)); // тиха операція
        Console.WriteLine($"Подій отримано: {changes} (+1 лише від лямбди-лічильника)");

        Console.WriteLine();
        Console.WriteLine("=== FindAll із Func ===");
        Console.WriteLine($" Вільних примірників: {service.FindAll(c => !c.IsIssued).Count}");
        Console.WriteLine($" Виданих примірників: {service.FindAll(c => c.IsIssued).Count}");
        Console.WriteLine($"Лог: {logPath}");
    }
}