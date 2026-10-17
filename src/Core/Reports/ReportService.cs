using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    // 1) топ-N читачів: групування + власний метод-розширення
    public static IReadOnlyList<ReaderLoanCount> TopReaders(IEnumerable<Loan> source, int n) =>
        source.GroupBy(l => l.ReaderId, StringComparer.OrdinalIgnoreCase)
              .Select(g => new ReaderLoanCount(g.Key, g.Count(), g.Count(l => l.IsOpen)))
              .TopBy(r => r.Loans, n, r => r.ReaderId)
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

    // 3) групування видань за десятиліттям (методний синтаксис)
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

    // 5) GroupJoin: усі видання, а до кожного його примірники (видання без примірників теж лишаються)
    public static IReadOnlyList<BookAvailabilityLine> BookAvailability(
        IEnumerable<Book> books, IEnumerable<BookCopy> copies) =>
        books.GroupJoin(copies, b => b.Isbn, c => c.Isbn,
                        (b, cs) => new BookAvailabilityLine(b.Isbn, b.Title, cs.Count(), cs.Count(c => !c.IsIssued)))
             .OrderBy(x => x.Free)
             .ThenBy(x => x.Isbn, StringComparer.Ordinal)
             .ToList();

    // 6) Join + GroupBy + Join: видачі -> примірники -> видання, потім топ-N
    public static IReadOnlyList<TitleLoanCount> PopularTitles(
        IEnumerable<Loan> loans, IEnumerable<BookCopy> copies, IEnumerable<Book> books, int n) =>
        loans.Join(copies, l => l.CopyId, c => c.Id, (l, c) => c.Isbn)
             .GroupBy(isbn => isbn)
             .Join(books, g => g.Key, b => b.Isbn, (g, b) => new TitleLoanCount(b.Isbn, b.Title, g.Count()))
             .TopBy(t => t.Loans, n, t => t.Isbn)
             .ToList();

    // 7) те саме, що BooksByDecade, але в синтаксисі запитів
    public static IReadOnlyList<GroupSummary> BooksByDecadeQuery(IEnumerable<Book> source) =>
        (from b in source
         group b by b.Year / 10 * 10 into g
         orderby g.Key
         select new GroupSummary($"{g.Key}-ті", g.Count()))
        .ToList();
}