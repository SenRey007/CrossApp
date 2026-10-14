using Core.Events;

namespace Cli;

internal static class LendingMessages
{
    public static string Format(LendingChangedEventArgs e)
    {
        string text = e.Kind switch
        {
            LendingChangeKind.BookAdded => $"додано примірник {e.CopyId}",
            LendingChangeKind.CopyIssued =>
                $"примірник {e.CopyId} видано читачу {e.ReaderId} до {e.DueDate:yyyy-MM-dd}",
            LendingChangeKind.CopyReturned =>
                $"примірник {e.CopyId} повернено читачем {e.ReaderId}",
            _ => e.Kind.ToString()
        };
        return $"{e.At:HH:mm:ss} {e.Kind,-12} {text}";
    }
}