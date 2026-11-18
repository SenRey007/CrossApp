using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonBookCatalogFormat : IBookCatalogFormat
{
    public string Extension => ".json";

    public void Save(string path, IEnumerable<Book> books) =>
        JsonFile.Save(path, books.Select(b => b.ToDto()));

    public IReadOnlyList<Book> Load(string path) =>
        JsonFile.Load<BookDto, Book>(path, Book.FromDto);
}