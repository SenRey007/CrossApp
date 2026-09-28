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
    public LoanStatus Status { get; private set; }
    public bool IsOpen => Status == LoanStatus.Open;
    public bool IsClosed => Status == LoanStatus.Returned;

    private Loan(string id, BookCopy copy, string readerId, DateOnly issuedOn,
        DateOnly? returnedOn, LoanStatus status)
    {
        Id = id;
        _copy = copy;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        ReturnedOn = returnedOn;
        Status = status;
    }

    public static Loan Open(string id, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        ValidateCommon(id, copy, readerId);

        copy.Issue(); // кине виняток, якщо примірник уже виданий
        return new Loan(id.Trim(), copy, readerId.Trim(), issuedOn, null, LoanStatus.Open);
    }

    public void Close(DateOnly returnedOn)
    {
        if (IsClosed)
            throw new InvalidOperationException(
                $"Видача {Id} вже закрита {ReturnedOn:yyyy-MM-dd}, закрити повторно не можна");
        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn,
                $"Дата повернення {returnedOn:yyyy-MM-dd} раніше дати видачі {IssuedOn:yyyy-MM-dd}");
        EnsureCanMoveTo(LoanStatus.Returned);

        _copy.Return();                 // спочатку дія, що може кинути виняток
        ReturnedOn = returnedOn;        // стан змінюємо в кінці
        Status = LoanStatus.Returned;
    }

    public void MarkLost()
    {
        EnsureCanMoveTo(LoanStatus.Lost);
        Status = LoanStatus.Lost;       // примірник лишається виданим
    }

    private void EnsureCanMoveTo(LoanStatus next)
    {
        if (!CanMove(Status, next))
            throw new InvalidOperationException(
                $"Видача {Id}: перехід зі стану {Status} у {next} неможливий");
    }

    // Таблиця допустимих переходів: читається як список правил.
    private static bool CanMove(LoanStatus current, LoanStatus next) => (current, next) switch
    {
        (LoanStatus.Open, LoanStatus.Returned) => true,
        (LoanStatus.Open, LoanStatus.Lost) => true,
        (LoanStatus.Lost, LoanStatus.Returned) => true,
        _ => false
    };

    public LoanDto ToDto() => new(Id, CopyId, ReaderId, IssuedOn, ReturnedOn, Status.ToString());

    public static Loan FromDto(LoanDto dto, BookCopy copy)
    {
        ValidateCommon(dto.Id, copy, dto.ReaderId);

        if (!Enum.TryParse(dto.Status, ignoreCase: true, out LoanStatus status) || !Enum.IsDefined(status))
            throw new ArgumentException($"Невідомий стан видачі '{dto.Status}'", nameof(dto));
        if (copy.Id != dto.CopyId)
            throw new ArgumentException(
                $"Видача {dto.Id} стосується примірника {dto.CopyId}, а передано {copy.Id}", nameof(copy));
        if (status == LoanStatus.Returned && dto.ReturnedOn is null)
            throw new ArgumentException(
                $"Видача {dto.Id} має стан Returned, але дата повернення відсутня", nameof(dto));
        if (status != LoanStatus.Returned && dto.ReturnedOn is not null)
            throw new ArgumentException(
                $"Видача {dto.Id} у стані {status} не може мати дату повернення", nameof(dto));
        if (dto.ReturnedOn is { } returned && returned < dto.IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(dto), returned,
                $"Дата повернення {returned:yyyy-MM-dd} раніше дати видачі {dto.IssuedOn:yyyy-MM-dd}");
        if (status != LoanStatus.Returned && !copy.IsIssued)
            throw new InvalidOperationException(
                $"Видача {dto.Id} не завершена, але примірник {copy.Id} позначений як вільний");

        return new Loan(dto.Id.Trim(), copy, dto.ReaderId.Trim(), dto.IssuedOn, dto.ReturnedOn, status);
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