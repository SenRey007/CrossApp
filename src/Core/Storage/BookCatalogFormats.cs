using Core.Abstractions;

namespace Core.Storage;

public static class BookCatalogFormats
{
    public static IBookCatalogFormat Create(string name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "json" => new JsonBookCatalogFormat(),
        "csv" => new CsvBookCatalogFormat(),
        _ => throw new ArgumentException($"Невідомий формат '{name}': очікую json або csv.", nameof(name))
    };
}