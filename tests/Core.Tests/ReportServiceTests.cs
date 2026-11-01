using System.Globalization;
using Core.Reports;

namespace Core.Tests;

public class ReportServiceTests
{
    [Theory]
    [InlineData("2026-10-15", 0)] // строк = сьогодні: ще не прострочено
    [InlineData("2026-10-16", 1)]
    [InlineData("2026-12-01", 1)] // закрита видача не рахується ніколи
    public void Overdue_CountsOnlyOpenLoansPastDue(string today, int expected)
    {
        var date = DateOnly.Parse(today, CultureInfo.InvariantCulture);

        var result = ReportService.Overdue(TestData.LoansForOverdue(), date, periodDays: 14);

        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public void TopReaders_ResolvesTiesByReaderId()
    {
        var top = ReportService.TopReaders(TestData.LoansWithTie(), 2);

        Assert.Equal(new[] { "R-A", "R-B" }, top.Select(r => r.ReaderId));
        Assert.Equal(2, top[0].Loans);
    }
}