using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

/// <summary>
/// Файл із різнорідними рядками, тип визначається префіксом першої колонки:
///   B;id;isbn;title;year[;author]   — книга
///   R;id;fullName[;email]           — читач
/// </summary>
public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var books = new List<BookDto>();
        var readers = new List<ReaderDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Один switch — два різні типи результату (BookDto або ReaderDto).
            switch (ParseLine(line))
            {
                case ParsedBook b:
                    books.Add(b.Value);
                    break;
                case ParsedReader r:
                    readers.Add(r.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new MixedImportResult(books, readers, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["B" or "b", .. var fields] => ParseBook(fields),
            ["R" or "r", .. var fields] => ParseReader(fields),
            [var prefix, ..] => new ParseFailed($"невідомий префікс '{prefix}' (очікую B або R)"),
            _ => new ParseFailed("порожній рядок")
        };
    }

    // fields — колонки після префікса: id;isbn;title;year[;author]
    private static ParseOutcome ParseBook(string[] fields) => fields switch
    {
        { Length: < 4 } or { Length: > 5 }
            => new ParseFailed($"книга: очікую 4 або 5 полів після префікса, отримав {fields.Length}"),

        [_, "", _, ..] or [_, _, "", ..]
            => new ParseFailed("книга: ISBN або назва порожні"),

        [_, _, _, var year, ..] when
            !int.TryParse(year, NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
            || y < 1450 || y > DateTime.Now.Year
            => new ParseFailed($"книга: рік '{year}' поза допустимими межами"),

        [var id, var isbn, var title, var year, ..]
            => new ParsedBook(new BookDto(id, isbn, title,
                int.Parse(year, CultureInfo.InvariantCulture),
                fields.Length == 5 && fields[4] != "" ? fields[4] : null)),

        _ => new ParseFailed("книга: не вдалося розпізнати рядок")
    };

    // fields — колонки після префікса: id;fullName[;email]
    private static ParseOutcome ParseReader(string[] fields) => fields switch
    {
        { Length: < 2 } or { Length: > 3 }
            => new ParseFailed($"читач: очікую 2 або 3 поля після префікса, отримав {fields.Length}"),

        ["", ..] or [_, "", ..]
            => new ParseFailed("читач: id або ПІБ порожні"),

        [_, _, var email] when email != "" && !email.Contains('@')
            => new ParseFailed($"читач: email '{email}' не містить '@'"),

        [var id, var name]
            => new ParsedReader(new ReaderDto(id, name)),

        [var id, var name, var email]
            => new ParsedReader(new ReaderDto(id, name, email == "" ? null : email)),

        _ => new ParseFailed("читач: не вдалося розпізнати рядок")
    };

    private abstract record ParseOutcome;
    private sealed record ParsedBook(BookDto Value) : ParseOutcome;
    private sealed record ParsedReader(ReaderDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
