using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class LendingService(IBookCopyStore store, INotificationSink? sink = null, int? loanDays = null)
{
    public const int MaxOpenLoansPerReader = 5;
    public const int LoanPeriodDays = 14;

    private readonly IBookCopyStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly List<Loan> _loans = [];

    public event EventHandler<LendingChangedEventArgs>? Changed;

    // Єдине місце, де піднімається подія.
    private void OnChanged(string copyId, LendingChangeKind kind,
    string? readerId = null, DateOnly? dueDate = null)
{
    var args = new LendingChangedEventArgs(copyId, kind, readerId, dueDate);
    Changed?.Invoke(this, args);
    _sink?.Notify(args);
}

    public BookCopy AddBook(string isbn)
    {
        BookCopy copy = BookCopy.Create(Guid.NewGuid().ToString("N")[..8], isbn);
        _store.Add(copy);
        OnChanged(copy.Id, LendingChangeKind.BookAdded); // після зміни, не до
        return copy;
    }

    public int CountOpenLoans(string readerId) =>
        _loans.Count(l => l.ReaderId == readerId && l.IsOpen);

    public Loan IssueCopy(string loanId, string copyId, string readerId, DateOnly issuedOn)
    {
        BookCopy copy = _store.GetById(copyId)
            ?? throw new DomainException($"Немає примірника з id={copyId}.");

        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));
        string reader = readerId.Trim();

        int open = CountOpenLoans(reader);
        if (open >= MaxOpenLoansPerReader)
            throw new DomainException(
                $"Читач {reader} уже має {open} відкритих видач (максимум {MaxOpenLoansPerReader})");

        Loan loan = Loan.Open(loanId, copy, reader, issuedOn); // copy.Issue() всередині
        _store.Update(copy);
        _loans.Add(loan);

        OnChanged(copy.Id, LendingChangeKind.CopyIssued, reader, issuedOn.AddDays(_loanDays));
        return loan;
    }

    public void ReturnCopy(string loanId, DateOnly returnedOn)
    {
        Loan loan = _loans.FirstOrDefault(l => l.Id == loanId)
            ?? throw new DomainException($"Немає видачі з id={loanId}.");

        loan.Close(returnedOn); // copy.Return() всередині

        BookCopy copy = _store.GetById(loan.CopyId)!;
        _store.Update(copy);

        OnChanged(copy.Id, LendingChangeKind.CopyReturned, loan.ReaderId);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();
    public BookCopy? Find(string copyId) => _store.GetById(copyId);

    // Критерій задає той, хто викликає: тут потрібне значення назад, тому Func, а не подія.
    public IReadOnlyList<BookCopy> FindAll(Func<BookCopy, bool> predicate) =>
        _store.Find(predicate);

	private readonly INotificationSink? _sink = sink;

private readonly int _loanDays = loanDays switch
{
    null => LoanPeriodDays,
    > 0 => loanDays.Value,
    _ => throw new ArgumentOutOfRangeException(
        nameof(loanDays), loanDays,
        "Строк видачі має бути більшим за нуль")
};

   public async Task<int> ImportAsync(ICopyImporter importer, string path,
    CancellationToken ct = default, IProgress<int>? progress = null)
{
    ArgumentNullException.ThrowIfNull(importer);
    ct.ThrowIfCancellationRequested();

    IReadOnlyList<BookCopy> items = await importer.LoadAsync(path, ct);

    int added = 0;
    int processed = 0;
    foreach (BookCopy item in items)
    {
        ct.ThrowIfCancellationRequested();
        processed++;

        if (_store.GetById(item.Id) is null) // ідемпотентність
        {
            _store.Add(item);
            OnChanged(item.Id, LendingChangeKind.BookAdded);
            added++;
        }

        progress?.Report((int)(100L * processed / items.Count)); // відсотки
    }

    return added;
}
public async Task<int> ImportStreamAsync(ICopyStreamImporter importer, string path,
    CancellationToken ct = default)
{
    ArgumentNullException.ThrowIfNull(importer);
    ct.ThrowIfCancellationRequested();

    int added = 0;
    await foreach (BookCopy item in importer.StreamAsync(path).WithCancellation(ct))
    {
        ct.ThrowIfCancellationRequested(); // сервіс сам перевіряє токен між записами
        if (_store.GetById(item.Id) is not null)
            continue;

        _store.Add(item);
        OnChanged(item.Id, LendingChangeKind.BookAdded);
        added++;
    }

    return added;
}
public async Task<int> ImportWithTimeoutAsync(ICopyImporter importer, string path,
    TimeSpan timeout, CancellationToken userToken = default)
{
    using var timeoutCts = new CancellationTokenSource(timeout);
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(userToken, timeoutCts.Token);

    try
    {
        return await ImportAsync(importer, path, linked.Token);
    }
    catch (OperationCanceledException ex)
        when (timeoutCts.IsCancellationRequested && !userToken.IsCancellationRequested)
    {
        // Скасував саме таймаут, а не користувач.
        throw new TimeoutException($"Імпорт перевищив ліміт {timeout.TotalMilliseconds:F0} мс.", ex);
    }
}
}