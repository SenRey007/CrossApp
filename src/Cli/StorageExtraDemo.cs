using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;


namespace Cli;

public static class StorageExtraDemo
{
    public static void Run()
    {
        PartCaching();
        Console.WriteLine();
        PartSearch();
        Console.WriteLine();
        PartFactory();
    }

    private static void PartCaching()
    {
        Console.WriteLine("=== Додаткове 1: CachingBookCopyStore (декоратор) ===");
        var inner = new InMemoryBookCopyStore(SampleData.Copies());
        var cached = new CachingBookCopyStore(inner);

        cached.List();
        cached.List();
        cached.List();
        Console.WriteLine($" Три виклики List() через кеш -> звернень до inner.List(): {cached.ListCallsToInner}");

        cached.Add(BookCopy.Create("C-900", "978-966-03-1234-5"));
        cached.List();
        Console.WriteLine($" Після Add і ще одного List() -> звернень до inner.List(): {cached.ListCallsToInner}");
    }

    private static void PartSearch()
    {
        Console.WriteLine("=== Додаткове 2: пошук через Func<BookCopy, bool> ===");
        IBookCopyStore store = new InMemoryBookCopyStore(SampleData.Copies());
        IReadOnlyList<BookCopy> free = store.Find(c => !c.IsIssued);
        Console.WriteLine($" Вільних примірників: {free.Count} з {store.List().Count}");

        var service = new LendingService(store);
        service.IssueCopy("L-950", free[0].Id, "R-950", new DateOnly(2026, 10, 5));
        IReadOnlyList<BookCopy> freeNow = store.Find(c => !c.IsIssued);
        Console.WriteLine($" Після видачі одного примірника вільних: {freeNow.Count}");
    }

    private static void PartFactory()
    {
        Console.WriteLine("=== Додаткове 3: StoreFactory ===");
        IBookCopyStore plain = StoreFactory.Create([]);
        Console.WriteLine($" StoreFactory.Create([]) -> {plain.GetType().Name}");

        IBookCopyStore cached = StoreFactory.Create(["--cache"]);
        Console.WriteLine($" StoreFactory.Create([\"--cache\"]) -> {cached.GetType().Name}");

        IBookCopyStore cachedFile = StoreFactory.Create(["--file", "--cache"]);
        Console.WriteLine($" StoreFactory.Create([\"--file\",\"--cache\"]) -> {cachedFile.GetType().Name}");
    }
}