using System.Globalization;
using System.Text;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class CsvBookCatalogFormat : IBookCatalogFormat
{
    private const char Separator = ';';
    private const string Header = "id;isbn;title;year;author";

    public string Extension => ".csv";

    public void Save(string path, IEnumerable<Book> books)
    {
        var text = new StringBuilder();
        text.AppendLine(Header);
        foreach (Book b in books)
        {
            text.AppendLine(string.Join(Separator,
                Escape(b.Id), Escape(b.Isbn), Escape(b.Title),
                b.Year.ToString(CultureInfo.InvariantCulture), Escape(b.Author ?? "")));
        }

        AtomicFile.WriteAllText(path, text.ToString());
    }

    public IReadOnlyList<Book> Load(string path)
    {
        string full = Path.GetFullPath(path);
        if (!File.Exists(full))
            return [];

        string[] lines = File.ReadAllLines(full, Encoding.UTF8);
        var result = new List<Book>();
        int number = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (i == 0 && line.Trim().Equals(Header, StringComparison.OrdinalIgnoreCase))
                continue; // рядок заголовків

            number++;
            string[] f = Split(line);
            if (f.Length is not (4 or 5))
                throw new DomainException($"Запис {number}: очікую 4 або 5 колонок, отримав {f.Length}.");
            if (!int.TryParse(f[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
                throw new DomainException($"Запис {number}: рік '{f[3]}' не є числом.");

            try
            {
                string? author = f.Length == 5 && f[4] != "" ? f[4] : null;
                result.Add(Book.FromDto(new BookDto(f[0], f[1], f[2], year, author)));
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {number}: {ex.Message.ReplaceLineEndings(" ")}", ex);
            }
        }

        return result;
    }

    // Поле з «;» або лапками береться в лапки, лапки подвоюються. Розриви рядків у полях не підтримуються.
    private static string Escape(string value)
    {
        string clean = value.ReplaceLineEndings(" ");
        return clean.Contains(Separator) || clean.Contains('"')
            ? "\"" + clean.Replace("\"", "\"\"") + "\""
            : clean;
    }

    private static string[] Split(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == Separator)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}