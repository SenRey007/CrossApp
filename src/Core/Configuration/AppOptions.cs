namespace Core.Configuration;

public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public LibraryOptions Library { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "catalog.json";
    public string BooksFileName { get; init; } = "books.json";

    // Відносний DataPath рахується від baseDir, абсолютний береться як є.
    public string ResolveFullPath(string baseDir) => Resolve(baseDir, FileName);

    public string ResolveBooksPath(string baseDir) => Resolve(baseDir, BooksFileName);

    private string Resolve(string baseDir, string fileName)
    {
        string root = Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);
        return Path.GetFullPath(Path.Combine(root, fileName));
    }
}

public sealed class LibraryOptions
{
    public int LoanDays { get; init; } = 14;
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
}