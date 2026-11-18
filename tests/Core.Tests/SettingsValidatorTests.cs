using Core.Configuration;

namespace Core.Tests;

public class SettingsValidatorTests
{
    private static IReadOnlyList<string> Validate(
        string dataPath = "data", string fileName = "catalog.json", string booksFileName = "books.json",
        string format = "json", int loanDays = 14, string level = "Information") =>
        SettingsValidator.Validate(
            new StorageOptions { DataPath = dataPath, FileName = fileName, BooksFileName = booksFileName, Format = format },
            new LibraryOptions { LoanDays = loanDays },
            new LoggingOptions { MinimumLevel = level });

    [Fact]
    public void DefaultSettings_AreValid() => Assert.Empty(Validate());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyDataPath_IsReported(string dataPath) =>
        Assert.Contains(Validate(dataPath: dataPath), e => e.Contains("Storage:DataPath"));

    [Fact]
    public void FileNameWithoutJsonExtension_IsReported() =>
        Assert.Contains(Validate(fileName: "catalog.txt"), e => e.Contains("Storage:FileName"));

    [Fact]
    public void CsvFormat_RequiresCsvBooksFileName()
    {
        Assert.Contains(Validate(format: "csv"), e => e.Contains("Storage:BooksFileName"));
        Assert.Empty(Validate(format: "csv", booksFileName: "books.csv"));
    }

    [Fact]
    public void UnknownFormat_IsReported() =>
        Assert.Contains(Validate(format: "xml"), e => e.Contains("Storage:Format"));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveLoanDays_IsReported(int days) =>
        Assert.Contains(Validate(loanDays: days), e => e.Contains("Library:LoanDays"));
}