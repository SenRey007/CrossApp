using Core.Domain;
using Core.Dto;

namespace Cli;

public static class DomainDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Сценарій 1: успіх ===");
        BookCopy copy = BookCopy.Create("C-001", "978-966-03-1234-5");
        Console.WriteLine(copy);
        Loan loan = Loan.Open("L-001", copy, "R-001", new DateOnly(2026, 9, 1));
        Console.WriteLine(loan);
        Console.WriteLine(copy);
        loan.Close(new DateOnly(2026, 9, 15));
        Console.WriteLine(loan);
        Console.WriteLine(copy);
        Console.WriteLine();

        Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
        Loan active = Loan.Open("L-002", copy, "R-002", new DateOnly(2026, 9, 20));

        TryDo("повторна видача виданого примірника",
            () => Loan.Open("L-003", copy, "R-003", new DateOnly(2026, 9, 21)));
        TryDo("порожній ISBN",
            () => BookCopy.Create("C-003", " "));
        TryDo("повернення раніше видачі",
            () => active.Close(new DateOnly(2026, 9, 10)));
        TryDo("закрити вже закриту видачу",
            () => loan.Close(new DateOnly(2026, 9, 20)));
        TryDo("порожній читач",
            () => Loan.Open("L-004", BookCopy.Create("C-004", "978-0-00-000000-0"), " ", new DateOnly(2026, 9, 22)));

        Console.WriteLine("Після всіх відмов:");
        Console.WriteLine($" {active}");
        Console.WriteLine($" {copy}");
        Console.WriteLine();

        Console.WriteLine("=== Сценарій 3: ToDto / FromDto ===");
        BookCopyDto copyDto = copy.ToDto();
        Console.WriteLine($" {copyDto}");
        BookCopy restored = BookCopy.FromDto(copyDto);
        Console.WriteLine($" {restored}");
        TryDo("FromDto з порожнім ISBN",
            () => BookCopy.FromDto(new BookCopyDto("C-009", "", false)));
    }

    internal static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($" {title}: виняток НЕ спрацював — інваріант відсутній!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
        }
    }
}