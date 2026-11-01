using Core.Domain;

namespace Core.Tests;

internal static class TestData
{
    public static BookCopy Copy(string id, string isbn = "978-966-03-1234-5") =>
        BookCopy.Create(id, isbn);

    // Одна відкрита видача (строк 15.10) і одна закрита, яка не повинна рахуватись.
    public static IReadOnlyList<Loan> LoansForOverdue()
    {
        Loan open = Loan.Open("L-1", Copy("C-1"), "R-1", new DateOnly(2026, 10, 1));
        Loan closed = Loan.Open("L-2", Copy("C-2"), "R-1", new DateOnly(2026, 9, 1));
        closed.Close(new DateOnly(2026, 9, 5));
        return [open, closed];
    }

    // R-B за порядком стоїть першим, але за ключем (нічия по 2 видачі) має йти після R-A.
    public static IReadOnlyList<Loan> LoansWithTie()
    {
        var day = new DateOnly(2026, 10, 1);
        return
        [
            Loan.Open("L-1", Copy("C-1"), "R-B", day),
            Loan.Open("L-2", Copy("C-2"), "R-B", day),
            Loan.Open("L-3", Copy("C-3"), "R-A", day),
            Loan.Open("L-4", Copy("C-4"), "R-A", day),
            Loan.Open("L-5", Copy("C-5"), "R-C", day),
        ];
    }
}