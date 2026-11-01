using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCopyImporter : ICopyImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
    {
        // До відкриття файлу: скасований токен не має впертись у FileNotFoundException.
        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);

        List<BookCopyDto?> dtos = await JsonSerializer.DeserializeAsync<List<BookCopyDto?>>(stream, Options, ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<BookCopy>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            BookCopyDto? dto = dtos[i];
            if (dto is null)
                throw new DomainException($"Запис {i + 1}: порожній елемент.");

            try
            {
                result.Add(BookCopy.FromDto(dto));
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message.ReplaceLineEndings(" ")}", ex);
            }
        }

        return result;
    }
}