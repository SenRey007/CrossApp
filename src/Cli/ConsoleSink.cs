using Core.Abstractions;
using Core.Events;

namespace Cli;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(LendingChangedEventArgs e) =>
        Console.WriteLine(LendingMessages.Format(e));
}