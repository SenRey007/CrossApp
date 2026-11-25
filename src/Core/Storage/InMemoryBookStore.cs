using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryBookStore(IEnumerable<Book>? seed = null) : IBookStore
{
    // Singleton + паралельні HTTP-запити: усе під lock.
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
            EnsureIsbnIsFree(item);
            _items.Add(item.Id, item);
        }
    }

    public void Update(Book item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_sync)
        {
            EnsureIsbnIsFree(item);
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

    // Викликати лише всередині lock. ISBN порівнюємо за цифрами, тому з дефісами і без вони однакові.
    private void EnsureIsbnIsFree(Book item)
    {
        string isbn = Digits(item.Isbn);
        bool taken = _items.Values.Any(b =>
            !string.Equals(b.Id, item.Id, StringComparison.OrdinalIgnoreCase) && Digits(b.Isbn) == isbn);

        if (taken)
            throw new DomainException($"Видання з ISBN {item.Isbn} уже існує.");
    }

    private static string Digits(string isbn) => new(isbn.Where(char.IsDigit).ToArray());
}