using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCopyStreamImporter : ICopyStreamImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async IAsyncEnumerable<BookCopy> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);

        int number = 0;
        await foreach (BookCopyDto? dto in
            JsonSerializer.DeserializeAsyncEnumerable<BookCopyDto?>(stream, Options, ct))
        {
            number++;
            if (dto is null)
                throw new DomainException($"Запис {number}: порожній елемент.");

            BookCopy copy;
            try
            {
                copy = BookCopy.FromDto(dto);
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {number}: {ex.Message.ReplaceLineEndings(" ")}", ex);
            }

            yield return copy; // віддаємо запис одразу, не чекаючи кінця файлу
        }
    }
}