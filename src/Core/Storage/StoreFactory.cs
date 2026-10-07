using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static IBookCopyStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");
        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

        IBookCopyStore store = useFile
            ? new FileBookCopyStore(dataPath)
            : new InMemoryBookCopyStore(SampleData.Copies());

        return useCache ? new CachingBookCopyStore(store) : store;
    }
}