using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryBookStore(IEnumerable<Book>? seed = null) : IBookStore
{
    // Сховище живе як Singleton, а запити HTTP йдуть паралельно: Dictionary не потокобезпечний, тож усе під lock.
    private readonly object _sync = new();
    private readonly Dictionary<string, Book> _items =
        (seed ?? []).ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Book> List()
    {
        lock (_sync)
        {
            return _items.Values.ToList();
        }
    }

    public Book? GetById(string id)
    {
        lock (_sync)
        {
            return _items.GetValueOrDefault(id);
        }
    }

    public void Add(Book item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_sync)
        {
            if (_items.ContainsKey(item.Id))
                throw new DomainException($"Книга з id={item.Id} уже існує.");
            _items.Add(item.Id, item);
        }
    }

    public void Update(Book item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_sync)
        {
            _items[item.Id] = item;
        }
    }

    public bool Remove(string id)
    {
        lock (_sync)
        {
            return _items.Remove(id);
        }
    }
}