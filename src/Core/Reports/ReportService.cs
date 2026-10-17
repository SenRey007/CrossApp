using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    // 1) топ-N читачів: групування + стабільний порядок (другий ключ)
    public static IReadOnlyList<ReaderLoanCount> TopReaders(IEnumerable<Loan> source, int n) =>
        source.GroupBy(l => l.ReaderId, StringComparer.OrdinalIgnoreCase)
              .Select(g => new ReaderLoanCount(g.Key, g.Count(), g.Count(l => l.IsOpen)))
              .OrderByDescending(r => r.Loans)
              .ThenBy(r => r.ReaderId, StringComparer.Ordinal)
              .Take(n)
              .ToList();

    // 2) прострочені видачі: фільтр спочатку, потім проєкція в DTO
    public static IReadOnlyList<OverdueLine> Overdue(IEnumerable<Loan> source, DateOnly today, int periodDays) =>
        source.Where(l => l.IsOpen)
              .Select(l => new { Loan = l, Due = l.IssuedOn.AddDays(periodDays) })
              .Where(x => x.Due < today)
              .Select(x => new OverdueLine(x.Loan.Id, x.Loan.CopyId, x.Loan.ReaderId,
                                           x.Due, today.DayNumber - x.Due.DayNumber))
              .OrderByDescending(o => o.DaysOverdue)
              .ThenBy(o => o.LoanId, StringComparer.Ordinal)
              .ToList();

    // 3) групування видань за десятиліттям
    public static IReadOnlyList<GroupSummary> BooksByDecade(IEnumerable<Book> source) =>
        source.GroupBy(b => b.Year / 10 * 10)
              .OrderBy(g => g.Key)
              .Select(g => new GroupSummary($"{g.Key}-ті", g.Count()))
              .ToList();

    // 4) агрегати з обробкою порожньої послідовності
    public static CopyStats CopyTotals(IEnumerable<BookCopy> source)
    {
        var list = source.ToList();
        if (list.Count == 0)
            return new CopyStats(0, 0, 0, 0d, 0);

        var perTitle = list.GroupBy(c => c.Isbn, StringComparer.OrdinalIgnoreCase)
                           .Select(g => g.Count())
                           .ToList();

        return new CopyStats(perTitle.Count, list.Count, list.Count(c => c.IsIssued),
                             perTitle.Average(), perTitle.Max());
    }
}