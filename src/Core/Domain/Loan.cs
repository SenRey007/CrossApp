using Core.Dto;

namespace Core.Domain;

public sealed class Loan
{
    private readonly BookCopy _copy;

    public string Id { get; }
    public string CopyId => _copy.Id;
    public string ReaderId { get; }
    public DateOnly IssuedOn { get; }
    public DateOnly? ReturnedOn { get; private set; }
    public bool IsClosed => ReturnedOn is not null;

    private Loan(string id, BookCopy copy, string readerId, DateOnly issuedOn, DateOnly? returnedOn)
    {
        Id = id;
        _copy = copy;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        ReturnedOn = returnedOn;
    }

    public static Loan Open(string id, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        ValidateCommon(id, copy, readerId);

        copy.Issue(); // кине виняток, якщо примірник уже виданий
        return new Loan(id.Trim(), copy, readerId.Trim(), issuedOn, null);
    }

    public void Close(DateOnly returnedOn)
    {
        if (IsClosed)
            throw new InvalidOperationException(
                $"Видача {Id} вже закрита {ReturnedOn:yyyy-MM-dd}, закрити повторно не можна");
        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn,
                $"Дата повернення {returnedOn:yyyy-MM-dd} раніше дати видачі {IssuedOn:yyyy-MM-dd}");

        _copy.Return();          // спочатку дія, що може кинути виняток
        ReturnedOn = returnedOn; // стан змінюємо в кінці
    }

    public LoanDto ToDto() => new(Id, CopyId, ReaderId, IssuedOn, ReturnedOn);

    public static Loan FromDto(LoanDto dto, BookCopy copy)
    {
        ValidateCommon(dto.Id, copy, dto.ReaderId);

        if (copy.Id != dto.CopyId)
            throw new ArgumentException(
                $"Видача {dto.Id} стосується примірника {dto.CopyId}, а передано {copy.Id}", nameof(copy));
        if (dto.ReturnedOn is { } returned && returned < dto.IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(dto), returned,
                $"Дата повернення {returned:yyyy-MM-dd} раніше дати видачі {dto.IssuedOn:yyyy-MM-dd}");
        if (dto.ReturnedOn is null && !copy.IsIssued)
            throw new InvalidOperationException(
                $"Видача {dto.Id} відкрита, але примірник {copy.Id} позначений як вільний");

        return new Loan(dto.Id.Trim(), copy, dto.ReaderId.Trim(), dto.IssuedOn, dto.ReturnedOn);
    }

    private static void ValidateCommon(string id, BookCopy copy, string readerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор видачі обов'язковий", nameof(id));
        ArgumentNullException.ThrowIfNull(copy);
        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));
    }

    public override string ToString() =>
        $"{Id}: примірник {CopyId}, читач {ReaderId}, з {IssuedOn:yyyy-MM-dd}" +
        (ReturnedOn is { } r ? $" по {r:yyyy-MM-dd}" : " (відкрита)");
}