using Core.Domain;

namespace Core.Reports;

// Лише для порівняння продуктивності: той самий запит із різним місцем ToList.
public static class ReportVariants
{
    // Без проміжного ToList: один прохід ланцюжком
    public static IReadOnlyList<OverdueLine> OverdueChained(IEnumerable<Loan> source, DateOnly today, int periodDays) =>
        ReportService.Overdue(source, today, periodDays);

    // З ToList у середині: усі відкриті видачі спершу збираються в проміжний список
    public static IReadOnlyList<OverdueLine> OverdueMaterialized(IEnumerable<Loan> source, DateOnly today, int periodDays)
    {
        var withDue = source.Where(l => l.IsOpen)
                            .Select(l => new { Loan = l, Due = l.IssuedOn.AddDays(periodDays) })
                            .ToList(); // проміжна матеріалізація

        return withDue.Where(x => x.Due < today)
                      .Select(x => new OverdueLine(x.Loan.Id, x.Loan.CopyId, x.Loan.ReaderId,
                                                   x.Due, today.DayNumber - x.Due.DayNumber))
                      .OrderByDescending(o => o.DaysOverdue)
                      .ThenBy(o => o.LoanId, StringComparer.Ordinal)
                      .ToList();
    }

    // Той самий запит, але БЕЗ завершального ToList: кожен перебір виконує його заново
    public static IEnumerable<OverdueLine> OverdueQuery(IEnumerable<Loan> source, DateOnly today, int periodDays) =>
        source.Where(l => l.IsOpen)
              .Select(l => new { Loan = l, Due = l.IssuedOn.AddDays(periodDays) })
              .Where(x => x.Due < today)
              .Select(x => new OverdueLine(x.Loan.Id, x.Loan.CopyId, x.Loan.ReaderId,
                                           x.Due, today.DayNumber - x.Due.DayNumber))
              .OrderByDescending(o => o.DaysOverdue)
              .ThenBy(o => o.LoanId, StringComparer.Ordinal);
}