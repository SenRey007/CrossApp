using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class LendingService(IBookCopyStore store)
{
    public const int MaxOpenLoansPerReader = 5;
    public const int LoanPeriodDays = 14;

    private readonly IBookCopyStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly List<Loan> _loans = [];

    public event EventHandler<LendingChangedEventArgs>? Changed;

    // Єдине місце, де піднімається подія.
    private void OnChanged(string copyId, LendingChangeKind kind,
        string? readerId = null, DateOnly? dueDate = null) =>
        Changed?.Invoke(this, new LendingChangedEventArgs(copyId, kind, readerId, dueDate));

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
            ?? throw new InvalidOperationException($"Немає примірника з id={copyId}.");

        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));
        string reader = readerId.Trim();

        int open = CountOpenLoans(reader);
        if (open >= MaxOpenLoansPerReader)
            throw new InvalidOperationException(
                $"Читач {reader} уже має {open} відкритих видач (максимум {MaxOpenLoansPerReader})");

        Loan loan = Loan.Open(loanId, copy, reader, issuedOn); // copy.Issue() всередині
        _store.Update(copy);
        _loans.Add(loan);

        OnChanged(copy.Id, LendingChangeKind.CopyIssued, reader, issuedOn.AddDays(LoanPeriodDays));
        return loan;
    }

    public void ReturnCopy(string loanId, DateOnly returnedOn)
    {
        Loan loan = _loans.FirstOrDefault(l => l.Id == loanId)
            ?? throw new InvalidOperationException($"Немає видачі з id={loanId}.");

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
}