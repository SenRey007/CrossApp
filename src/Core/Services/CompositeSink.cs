using Core.Abstractions;
using Core.Events;

namespace Core.Services;

public sealed class CompositeSink(params INotificationSink[] sinks) : INotificationSink
{
    public void Notify(LendingChangedEventArgs e)
    {
        foreach (INotificationSink sink in sinks)
            sink.Notify(e);
    }
}