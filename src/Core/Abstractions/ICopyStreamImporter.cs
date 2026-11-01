using Core.Domain;

namespace Core.Abstractions;

public interface ICopyStreamImporter
{
    IAsyncEnumerable<BookCopy> StreamAsync(string path, CancellationToken ct = default);
}