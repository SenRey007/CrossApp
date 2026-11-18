using Core.Domain;

namespace Core.Abstractions;

public interface IBookCatalogFormat
{
    string Extension { get; }                              // ".json" або ".csv"
    void Save(string path, IEnumerable<Book> books);
    IReadOnlyList<Book> Load(string path);
}