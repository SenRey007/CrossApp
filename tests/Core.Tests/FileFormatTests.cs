using System.Text.Json;
using Core.Domain;
using Core.Storage;

namespace Core.Tests;

public sealed class FileFormatTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"crossapp-tests-{Guid.NewGuid():N}");

    private string StorePath => Path.Combine(_dir, "catalog.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Add_ThenNewStoreInstance_ListReturnsSameCopy()
    {
        var first = new FileBookCopyStore(StorePath);
        first.Add(BookCopy.Create("C-1", "978-966-03-1234-5"));

        var second = new FileBookCopyStore(StorePath); // новий екземпляр читає дані з диска

        BookCopy copy = Assert.Single(second.List());
        Assert.Equal("C-1", copy.Id);
        Assert.Equal("978-966-03-1234-5", copy.Isbn);
        Assert.False(copy.IsIssued);
    }

    [Fact]
    public void Update_PersistsIssuedState_AcrossInstances()
    {
        var first = new FileBookCopyStore(StorePath);
        BookCopy copy = BookCopy.Create("C-1", "978-966-03-1234-5");
        first.Add(copy);
        copy.Issue();
        first.Update(copy);

        BookCopy? loaded = new FileBookCopyStore(StorePath).GetById("C-1");

        Assert.NotNull(loaded);
        Assert.True(loaded.IsIssued);
    }

    [Fact]
    public void Save_WritesReadableCyrillic_AndLeavesNoTempFile()
    {
        string path = Path.Combine(_dir, "books.json");
        var books = new[] { Book.Create("B-1", "978-966-03-1234-5", "Кобзар", 1840, "Тарас Шевченко") };

        JsonFile.Save(path, books.Select(b => b.ToDto()));

        string text = File.ReadAllText(path);
        Assert.Contains("Кобзар", text);
        Assert.DoesNotContain("\\u04", text);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void List_WithCorruptedJson_ThrowsJsonException()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(StorePath, "{ not json");
        var store = new FileBookCopyStore(StorePath);

        Assert.ThrowsAny<JsonException>(() => store.List());
    }

    [Fact]
    public void Csv_RoundTrip_PreservesBooks_IncludingSeparatorInsideTitle()
    {
        string path = Path.Combine(_dir, "books.csv");
        var format = new CsvBookCatalogFormat();
        var books = new[]
        {
            Book.Create("B-1", "978-966-03-1234-5", "Кобзар", 1840, "Тарас Шевченко"),
            Book.Create("B-2", "978-617-12-5678-9", "Назва; з крапкою з комою", 1912),
        };

        format.Save(path, books);
        IReadOnlyList<Book> loaded = format.Load(path);

        Assert.Equal(new[] { "B-1", "B-2" }, loaded.Select(b => b.Id));
        Assert.Equal("Назва; з крапкою з комою", loaded[1].Title);
        Assert.Equal("Тарас Шевченко", loaded[0].Author);
        Assert.Null(loaded[1].Author);
    }

    [Fact]
    public void Csv_Load_WithNonNumericYear_ThrowsDomainException_WithRecordNumber()
    {
        Directory.CreateDirectory(_dir);
        string path = Path.Combine(_dir, "books.csv");
        File.WriteAllText(path,
            "id;isbn;title;year;author\n" +
            "B-1;978-966-03-1234-5;Кобзар;1840;Тарас Шевченко\n" +
            "B-2;978-617-12-5678-9;Земля;абракадабра;\n");

        var ex = Assert.Throws<DomainException>(() => new CsvBookCatalogFormat().Load(path));

        Assert.Contains("Запис 2", ex.Message);
    }
}