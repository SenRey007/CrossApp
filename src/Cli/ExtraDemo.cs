using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Services;

namespace Cli;

public static class ExtraDemo
{
    public static void Run()
    {
        PartImport();
        Console.WriteLine();
        PartLimit();
        Console.WriteLine();
        PartStates();
    }

    private static void PartImport()
    {
        Console.WriteLine("=== Додаткове 1: імпорт → сутності ===");
        string path = Path.Combine("data", "extra.csv");
        if (!File.Exists(path))
        {
            Console.WriteLine($" Файл не знайдено: {Path.GetFullPath(path)}");
            return;
        }

        ImportResult<BookDto> imported = BookCsvImporter.Load(path);
        Console.WriteLine($" Імпорт (DTO): {imported.Stats}");

        ImportResult<Book> entities = BookEntityMapper.ToEntities(imported);
        Console.WriteLine($" Після доменних перевірок: {entities.Stats}");
        foreach (Book b in entities.Items)
            Console.WriteLine($"  {b}");
        foreach (string e in entities.Errors)
            Console.WriteLine($"  ! {e}");
    }

    private static void PartLimit()
    {
        Console.WriteLine("=== Додаткове 2: ліміт відкритих видач на читача ===");
        var service = new LendingService();
        var day = new DateOnly(2026, 9, 1);

        for (int i = 1; i <= LendingService.MaxOpenLoansPerReader; i++)
        {
            BookCopy c = BookCopy.Create($"C-{100 + i}", "978-966-03-1234-5");
            service.Issue($"L-{100 + i}", c, "R-010", day);
        }
        Console.WriteLine($" Відкритих видач у R-010: {service.CountOpenLoans("R-010")}");

        BookCopy extra = BookCopy.Create("C-106", "978-966-03-1234-5");
        DomainDemo.TryDo("6-та видача одному читачу",
            () => service.Issue("L-106", extra, "R-010", day));
        Console.WriteLine($" Примірник після відмови: {extra}");

        service.Return("L-101", day.AddDays(14));
        Console.WriteLine($" Після повернення L-101 відкритих: {service.CountOpenLoans("R-010")}");

        service.Issue("L-106", extra, "R-010", day.AddDays(15));
        Console.WriteLine($" 6-та видача після повернення: {extra}, відкритих: {service.CountOpenLoans("R-010")}");
    }

    private static void PartStates()
    {
        Console.WriteLine("=== Додаткове 3: стани видачі та переходи ===");
        BookCopy copy = BookCopy.Create("C-200", "978-966-03-1234-5");
        Loan loan = Loan.Open("L-200", copy, "R-020", new DateOnly(2026, 9, 1));
        Console.WriteLine($" Стан: {loan.Status}, примірник: {copy}");

        loan.MarkLost();
        Console.WriteLine($" Після MarkLost: {loan.Status}, примірник: {copy}");
        DomainDemo.TryDo("втрачена → втрачена", () => loan.MarkLost());

        loan.Close(new DateOnly(2026, 9, 20));
        Console.WriteLine($" Після Close (книгу знайшли): {loan.Status}, примірник: {copy}");
        DomainDemo.TryDo("повернена → втрачена", () => loan.MarkLost());
        DomainDemo.TryDo("повернена → повернена", () => loan.Close(new DateOnly(2026, 9, 21)));

        DomainDemo.TryDo("FromDto: Returned без дати",
            () => Loan.FromDto(new LoanDto("L-201", "C-200", "R-020", new DateOnly(2026, 9, 1), null, "Returned"), copy));
        DomainDemo.TryDo("FromDto: невідомий стан",
            () => Loan.FromDto(new LoanDto("L-202", "C-200", "R-020", new DateOnly(2026, 9, 1), null, "Pending"), copy));
    }
}