using Core.Domain;

namespace Core.Services;

public sealed class LendingService
{
    public const int MaxOpenLoansPerReader = 5;

    private readonly List<Loan> _loans = [];

    public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();

    public int CountOpenLoans(string readerId) =>
        _loans.Count(l => l.ReaderId == readerId && l.IsOpen);

    public Loan Issue(string loanId, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);
        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));
        string reader = readerId.Trim();

        // Правило «між сутностями» перевіряємо ДО зміни стану.
        int open = CountOpenLoans(reader);
        if (open >= MaxOpenLoansPerReader)
            throw new InvalidOperationException(
                $"Читач {reader} уже має {open} відкритих видач (максимум {MaxOpenLoansPerReader})");

        Loan loan = Loan.Open(loanId, copy, reader, issuedOn); // може кинути виняток, але стан ще не змінено
        _loans.Add(loan);
        return loan;
    }

    public void Return(string loanId, DateOnly returnedOn)
    {
        Loan loan = _loans.FirstOrDefault(l => l.Id == loanId)
            ?? throw new ArgumentException($"Видача {loanId} не знайдена", nameof(loanId));
        loan.Close(returnedOn);
    }
}