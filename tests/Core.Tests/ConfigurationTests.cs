using Core.Configuration;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ResolveFullPath_RelativeDataPath_IsCombinedWithBaseDirectory()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "app-base");
        var storage = new StorageOptions { DataPath = "data", FileName = "catalog.json" };

        string result = storage.ResolveFullPath(baseDir);

        Assert.Equal(Path.GetFullPath(Path.Combine(baseDir, "data", "catalog.json")), result);
    }

    [Fact]
    public void ResolveFullPath_RootedDataPath_IgnoresBaseDirectory()
    {
        string rooted = Path.Combine(Path.GetTempPath(), "elsewhere");
        var storage = new StorageOptions { DataPath = rooted, FileName = "catalog.json" };

        string result = storage.ResolveFullPath(Path.Combine(Path.GetTempPath(), "app-base"));

        Assert.Equal(Path.GetFullPath(Path.Combine(rooted, "catalog.json")), result);
    }

    [Fact]
    public void IssueCopy_UsesConfiguredLoanDays()
    {
        var service = new LendingService(new InMemoryBookCopyStore(), loanDays: 7);
        BookCopy copy = service.AddBook("978-966-03-1234-5");
        DateOnly? due = null;
        service.Changed += (_, e) => due = e.DueDate;

        service.IssueCopy("L-1", copy.Id, "R-1", new DateOnly(2026, 10, 1));

        Assert.Equal<DateOnly?>(new DateOnly(2026, 10, 8), due);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void LendingService_WithNonPositiveLoanDays_ThrowsArgumentOutOfRange(int days)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LendingService(new InMemoryBookCopyStore(), loanDays: days));
    }
}