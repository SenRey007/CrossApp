namespace Core.Events;

public sealed class LendingChangedEventArgs(
    string copyId,
    LendingChangeKind kind,
    string? readerId,
    DateOnly? dueDate) : EventArgs
{
    public string CopyId { get; } = copyId;
    public LendingChangeKind Kind { get; } = kind;
    public string? ReaderId { get; } = readerId;   // null для BookAdded
    public DateOnly? DueDate { get; } = dueDate;   // є лише для CopyIssued
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}