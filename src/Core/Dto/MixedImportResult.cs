namespace Core.Dto;

/// <summary>Результат імпорту файлу з рядками різних типів (книги + читачі).</summary>
public sealed record MixedImportResult(
    IReadOnlyList<BookDto> Books,
    IReadOnlyList<ReaderDto> Readers,
    IReadOnlyList<string> Errors)
{
    public ImportStats Stats => ImportStats.Create(Books.Count + Readers.Count, Errors.Count);
}
