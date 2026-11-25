using Core.Domain;
using Core.Storage;

namespace Core.Tests;

public class BookStoreTests
{
    private static Book NewBook(string id = "B-1") =>
        Book.Create(id, "978-966-03-1234-5", "Кобзар", 1840, "Тарас Шевченко");

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

        Parallel.For(0, 500, i => store.Add(NewBook($"B-{i}")));

        Assert.Equal(500, store.List().Count);
    }
}