using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class BookJsonImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ImportResult<BookDto> Load(string path)
    {
        var items = new List<BookDto>();
        var errors = new List<string>();

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("корінь JSON має бути масивом об'єктів");
                return new ImportResult<BookDto>(items, errors);
            }

            int number = 0;
            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                number++;

                switch (ParseElement(element))
                {
                    case ParseOk ok:
                        items.Add(ok.Value);
                        break;
                    case ParseFailed failed:
                        errors.Add($"елемент {number}: {failed.Reason}");
                        break;
                }
            }
        }
        catch (JsonException ex)
        {
            // Файл, який не є валідним JSON взагалі, — одна помилка, а не виняток.
            errors.Add($"некоректний JSON: {ex.Message}");
        }

        return new ImportResult<BookDto>(items, errors);
    }

    private static ParseOutcome ParseElement(JsonElement element)
    {
        BookDto? dto;
        try
        {
            dto = element.Deserialize<BookDto>(Options);
        }
        catch (JsonException ex)
        {
            // Наприклад, "year": "абракадабра" або елемент не є об'єктом.
            return new ParseFailed($"не вдалося прочитати об'єкт (поле {ex.Path ?? "?"})");
        }

        return dto switch
        {
            null
                => new ParseFailed("елемент дорівнює null"),

            { Id: null or "" }
                => new ParseFailed("порожній id"),

            { Isbn: null or "" } or { Title: null or "" }
                => new ParseFailed("ISBN або назва порожні"),

            { Year: var y } when y < 1450 || y > DateTime.Now.Year
                => new ParseFailed($"рік '{y}' поза допустимими межами"),

            _ => new ParseOk(dto)
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(BookDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
