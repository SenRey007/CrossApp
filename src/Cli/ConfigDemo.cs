using System.Text.Json;
using Core;
using Core.Domain;
using Core.Dto;
using Core.Storage;

namespace Cli;

public static class ConfigDemo
{
    public static int Run(string[] args)
    {
        AppSettings s = AppConfig.Load(args);
        bool debug = string.Equals(s.Logging.MinimumLevel, "Debug", StringComparison.OrdinalIgnoreCase);
        Action<string> log = text =>
        {
            if (debug)
                Console.WriteLine($"[debug] {text}");
        };

        Row("Профіль", $"{s.ProfileName} (EnvironmentName: {s.EnvironmentName})");
        Row("Рівень логу", s.Logging.MinimumLevel);
        Row("Файл даних", s.DataFile);
        Row("Файл видань", s.BooksFile);
        Row("Строк видачі", $"{s.Library.LoanDays} дн.");
        // Секрет: лише факт наявності й довжина, ніколи саме значення.
        Row("ApiKey", s.ApiKey is null ? "не заданий" : $"присутній, довжина {s.ApiKey.Length}");

        try
        {
            if (args.Contains("--export"))
                Export(s, log);
            else if (args.Contains("--load"))
                Load(s);

            return 0;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Файл пошкоджено (некоректний JSON): {ex.Message}");
            return 4;
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message}");
            return 4;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Не вдалося прочитати чи записати файл: {ex.Message}");
            return 1;
        }
    }

    private static void Row(string label, string value) => Console.WriteLine($"{label,-13}: {value}");

    private static void Export(AppSettings s, Action<string> log)
    {
        IReadOnlyList<Book> books = SampleData.Books();
        IReadOnlyList<BookCopy> copies = SampleData.Copies().ToList();

        log($"Записую {books.Count} видань у тимчасовий файл {s.BooksFile}.tmp, потім підміна");
        JsonFile.Save(s.BooksFile, books.Select(b => b.ToDto()));
        JsonFile.Save(s.DataFile, copies.Select(c => c.ToDto()));

        Console.WriteLine($"Експортовано видань: {books.Count} -> {Path.GetFileName(s.BooksFile)}");
        Console.WriteLine($"Експортовано примірників: {copies.Count} -> {Path.GetFileName(s.DataFile)}");
    }

    private static void Load(AppSettings s)
    {
        if (!File.Exists(s.BooksFile) || !File.Exists(s.DataFile))
        {
            Console.WriteLine("Файли даних не знайдено. Спершу виконайте --export.");
            return;
        }

        IReadOnlyList<Book> books = JsonFile.Load<BookDto, Book>(s.BooksFile, Book.FromDto);
        IReadOnlyList<BookCopy> copies = JsonFile.Load<BookCopyDto, BookCopy>(s.DataFile, BookCopy.FromDto);

        Console.WriteLine($"Завантажено видань: {books.Count}, примірників: {copies.Count}");
        foreach (Book b in books.Take(3))
            Console.WriteLine($" {b}");
    }
}