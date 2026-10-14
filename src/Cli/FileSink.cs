using Core.Abstractions;
using Core.Events;

namespace Cli;

public sealed class FileSink(string path) : INotificationSink
{
    public void Notify(LendingChangedEventArgs e)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, LendingMessages.Format(e) + Environment.NewLine);
    }
}