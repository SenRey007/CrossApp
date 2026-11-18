using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static IBookCopyStore Create(string[] args, string? dataFilePath = null)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        // Шлях має приходити з конфігурації; запасний варіант лише для викликів без неї.
        string path = dataFilePath ?? Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

        IBookCopyStore store = useFile
            ? new FileBookCopyStore(path)
            : new InMemoryBookCopyStore(SampleData.Copies());

        return useCache ? new CachingBookCopyStore(store) : store;
    }
}