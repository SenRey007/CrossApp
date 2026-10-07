using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingBookCopyStore(IBookCopyStore inner) : IBookCopyStore
{
    private readonly IBookCopyStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private IReadOnlyList<BookCopy>? _cache;

    public int ListCallsToInner { get; private set; }

    public IReadOnlyList<BookCopy> List()
    {
        if (_cache is null)
        {
            _cache = _inner.List();
            ListCallsToInner++;
        }
        return _cache;
    }

    public BookCopy? GetById(string id) =>
        List().FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public void Add(BookCopy item)
    {
        _inner.Add(item);
        Invalidate();
    }

    public void Update(BookCopy item)
    {
        _inner.Update(item);
        Invalidate();
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed) Invalidate();
        return removed;
    }

    private void Invalidate() => _cache = null;
}