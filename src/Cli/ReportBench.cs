using System.Diagnostics;
using System.Globalization;
using Core;
using Core.Domain;
using Core.Reports;
using Core.Services;

namespace Cli;

public static class ReportBench
{
    private const int RecordCount = 100_000;

    public static void Run()
    {
        string[] isbns = SampleData.Books().Select(b => b.Isbn).ToArray();
        DateOnly today = SampleData.ReferenceDate;
        int period = LendingService.LoanPeriodDays;

        // Генерація 100 000 видач через фабрики домену (кожна має свій примірник).
        var loans = new List<Loan>(RecordCount);
        for (int i = 0; i < RecordCount; i++)
        {
            BookCopy copy = BookCopy.Create($"G-{i:D6}", isbns[i % isbns.Length]);
            DateOnly issuedOn = today.AddDays(-(i % 90));
            Loan loan = Loan.Open($"GL-{i:D6}", copy, $"R-{i % 1000:D4}", issuedOn);
            if (i % 3 == 0)
                loan.Close(issuedOn.AddDays(5));
            loans.Add(loan);
        }

        int open = loans.Count(l => l.IsOpen);
        int overdue = ReportService.Overdue(loans, today, period).Count;
        Console.WriteLine($"Згенеровано видач: {RecordCount} (відкритих: {open}), прострочених: {overdue}");

        double chained = Measure(() => ReportVariants.OverdueChained(loans, today, period));
        double middle = Measure(() => ReportVariants.OverdueMaterialized(loans, today, period));
        double twice = Measure(() =>
        {
            var query = ReportVariants.OverdueQuery(loans, today, period);
            _ = query.Count();   // 1-й прохід
            _ = query.ToList();  // 2-й прохід
        });
        double once = Measure(() =>
        {
            var list = ReportVariants.OverdueQuery(loans, today, period).ToList(); // один прохід
            _ = list.Count;
        });

        Console.WriteLine($" Ланцюжок без проміжного ToList:         {Ms(chained),8} мс");
        Console.WriteLine($" З ToList у середині ланцюжка:           {Ms(middle),8} мс");
        Console.WriteLine($" Запит, перебраний двічі (Count+ToList): {Ms(twice),8} мс");
        Console.WriteLine($" Запит, матеріалізований один раз:       {Ms(once),8} мс");
    }

    // Один прогрівальний запуск (JIT), далі медіана із 7 замірів.
    private static double Measure(Action action, int runs = 7)
    {
        action();
        GC.Collect();

        var times = new double[runs];
        for (int i = 0; i < runs; i++)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            times[i] = sw.Elapsed.TotalMilliseconds;
        }

        Array.Sort(times);
        return times[runs / 2];
    }

    private static string Ms(double value) => value.ToString("F1", CultureInfo.InvariantCulture);
}