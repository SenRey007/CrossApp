using Core.Dto;

namespace Core.Import;

public static class BookCsvImporter
{
    // Роздільник — крапка з комою
    private const char Separator = ';';

    public static ImportResult<BookDto> Load(string path)
    {
        var items = new List<BookDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue; // рядок заголовків

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<BookDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
{
    string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

    return parts switch
    {
        { Length: < 4 } or { Length: > 5 }
            => new ParseFailed($"очікую 4 або 5 колонок, отримав {parts.Length}"),

        [_, "", _, _] or [_, _, "", _] or [_, "", _, _, _] or [_, _, "", _, _]
            => new ParseFailed("ISBN або назва порожні"),

        [_, _, _, var year4] when !int.TryParse(year4, out int y4) || y4 < 1450 || y4 > DateTime.Now.Year
            => new ParseFailed($"рік '{year4}' поза допустимими межами"),

        [_, _, _, var year5, _] when !int.TryParse(year5, out int y5) || y5 < 1450 || y5 > DateTime.Now.Year
            => new ParseFailed($"рік '{year5}' поза допустимими межами"),

        [var id, var isbn, var title, var year]
            => new ParseOk(new BookDto(id, isbn, title, int.Parse(year))),

        [var id, var isbn, var title, var year, var author]
            => new ParseOk(new BookDto(id, isbn, title, int.Parse(year), author)),

        _ => new ParseFailed($"не вдалося розпізнати рядок ({parts.Length} колонок)")
    };
}

    private abstract record ParseOutcome;
    private sealed record ParseOk(BookDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}