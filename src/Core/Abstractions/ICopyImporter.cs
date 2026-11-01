using Core.Domain;

namespace Core.Abstractions;

public interface ICopyImporter
{
    Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default);
}