using Core.Domain;

namespace Core.Abstractions;

public interface IBookCopyStore
{
    IReadOnlyList<BookCopy> List();
    BookCopy? GetById(string id);
    void Add(BookCopy item);
    void Update(BookCopy item);
    bool Remove(string id);
    IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate) =>
    	List().Where(predicate).ToList();
}