using Core.Domain;

namespace Core.Abstractions;

public interface IBookStore
{
    IReadOnlyList<Book> List();
    Book? GetById(string id);
    void Add(Book item);
    void Update(Book item);
    bool Remove(string id);
}