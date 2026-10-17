using System.Globalization;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class ReportsDemo
{
    public static int Run(string command)
    {
        // Джерела: примірники — зі сховища, видачі й видання — з тестових даних.
        IReadOnlyList<BookCopy> copies = SampleData.ReportCopies();
        IReadOnlyList<Loan> loans = SampleData.ReportLoans(copies); // частина примірників стає виданою
        IBookCopyStore store = new InMemoryBookCopyStore(copies);
        IReadOnlyList<Book> books = SampleData.Books();

        switch (command)
        {
            case "report-top":
                foreach (ReaderLoanCount r in ReportService.TopReaders(loans, 5))
                    Console.WriteLine($" {r.ReaderId,-6} видач: {r.Loans,2}  відкритих: {r.Open,2}");
                break;

            case "report-overdue":
                foreach (OverdueLine o in ReportService.Overdue(loans, SampleData.ReferenceDate, LendingService.LoanPeriodDays))
                    Console.WriteLine($" {o.LoanId,-5} {o.CopyId,-6} {o.ReaderId,-6} до {o.DueDate:yyyy-MM-dd}  прострочено на {o.DaysOverdue,3} дн.");
                break;

            case "report-groups":
                foreach (GroupSummary g in ReportService.BooksByDecade(books))
                    Console.WriteLine($" {g.Key,-8} видань: {g.Count,3}");
                break;

            case "report-sum":
            {
                CopyStats s = ReportService.CopyTotals(store.List());
                Console.WriteLine($" видань: {s.Titles}, примірників: {s.Copies} (виданих {s.IssuedCopies}), " +
                                  $"у середньому {Fmt(s.AverageCopiesPerTitle)} на видання, максимум {s.MaxCopiesPerTitle}");
                break;
            }

            case "report-deferred":
                DeferredDemo();
                break;

            case "report-empty":
            {
                CopyStats e = ReportService.CopyTotals([]);
                Console.WriteLine($" TopReaders: {ReportService.TopReaders([], 5).Count}");
                Console.WriteLine($" Overdue: {ReportService.Overdue([], SampleData.ReferenceDate, 14).Count}");
                Console.WriteLine($" BooksByDecade: {ReportService.BooksByDecade([]).Count}");
                Console.WriteLine($" CopyTotals: видань {e.Titles}, примірників {e.Copies}, середнє {Fmt(e.AverageCopiesPerTitle)}, максимум {e.MaxCopiesPerTitle}");
                break;
            }

            default:
                Console.WriteLine("report-top | report-overdue | report-groups | report-sum | report-deferred | report-empty");
                return 2;
        }

        return 0;
    }

    // Явна культура: "1.80", а не "1,80" залежно від локалі ОС (як у лабі 3).
    private static string Fmt(double value) => value.ToString("F2", CultureInfo.InvariantCulture);

    private static void DeferredDemo()
    {
        var source = SampleData.ReportCopies().ToList();
        SampleData.ReportLoans(source);                      // 10 примірників стають виданими

        var query = source.Where(c => !c.IsIssued);          // запит побудовано, НЕ виконано
        Console.WriteLine($"Вільних примірників до змін: {query.Count()}");

        source.Add(BookCopy.Create("RC-99", "978-966-03-1234-5"));
        Console.WriteLine($"Вільних примірників після змін: {query.Count()}"); // число інше

        var snapshot = query.ToList();                       // результат зафіксовано
        source.Clear();
        Console.WriteLine($"У знімку лишилось: {snapshot.Count} (а сам запит після Clear бачить: {query.Count()})");
    }
}