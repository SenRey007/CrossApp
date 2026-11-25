using Core.Domain;
using Core.Storage;

namespace Core.Tests;

public class BookStoreTests
{
    private static Book NewBook(string id = "B-1", string isbn = "978-966-03-1234-5") =>
        Book.Create(id, isbn, "Кобзар", 1840, "Тарас Шевченко");

    [Fact]
    public void Add_ThenGetById_ReturnsSameBook_IgnoringIdCase()
    {
        var store = new InMemoryBookStore();
        store.Add(NewBook());

        Book? found = store.GetById("b-1");

        Assert.NotNull(found);
        Assert.Equal("Кобзар", found.Title);
    }

    [Fact]
    public void Add_DuplicateId_ThrowsDomainException()
    {
        var store = new InMemoryBookStore([NewBook()]);

        Assert.Throws<DomainException>(() => store.Add(NewBook()));
    }

    [Fact]
    public void Add_SameIsbnUnderAnotherId_ThrowsDomainException_EvenWithoutHyphens()
    {
        var store = new InMemoryBookStore([NewBook()]);

        Assert.Throws<DomainException>(() => store.Add(NewBook("B-2", "9789660312345")));
    }

    [Fact]
    public void Update_WithIsbnOfAnotherBook_ThrowsDomainException()
    {
        var store = new InMemoryBookStore([NewBook("B-1"), NewBook("B-2", "978-617-12-5678-9")]);

        Assert.Throws<DomainException>(() => store.Update(NewBook("B-2", "978-966-03-1234-5")));
    }

    [Fact]
    public void Update_BookKeepingItsOwnIsbn_Succeeds()
    {
        var store = new InMemoryBookStore([NewBook()]);

        store.Update(Book.Create("B-1", "978-966-03-1234-5", "Кобзар (2-ге видання)", 1840, "Тарас Шевченко"));

        Assert.Equal("Кобзар (2-ге видання)", store.GetById("B-1")!.Title);
    }

    [Fact]
    public void Remove_ExistingBook_ReturnsTrue_ThenBookIsGone()
    {
        var store = new InMemoryBookStore([NewBook()]);

        Assert.True(store.Remove("B-1"));
        Assert.Null(store.GetById("B-1"));
        Assert.False(store.Remove("B-1"));
    }

    [Fact]
    public void ParallelAdd_FromManyThreads_KeepsAllBooks()
    {
        var store = new InMemoryBookStore();

        Parallel.For(0, 500, i => store.Add(NewBook($"B-{i}", $"978966030{i:D4}")));

        Assert.Equal(500, store.List().Count);
    }
}