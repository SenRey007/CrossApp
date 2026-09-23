using Core.Dto;
using Core.Import;

// Використання: Cli [шлях] [--mixed]
//   .csv  -> BookCsvImporter, .json -> BookJsonImporter
//   --mixed -> файл із префіксами типів (B;... книги, R;... читачі)
bool mixed = args.Contains("--mixed");
string path = args.FirstOrDefault(a => !a.StartsWith("--")) ?? Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

if (mixed)
{
    MixedImportResult m = MixedCsvImporter.Load(path);

    Console.WriteLine($"Книг: {m.Books.Count}, читачів: {m.Readers.Count}");
    foreach (BookDto b in m.Books.Take(3))
        Console.WriteLine($" [книга]  {b.Id,-6} {b.Isbn,-18} {b.Title,-30} {b.Year,5} {b.Author}");
    foreach (ReaderDto r in m.Readers.Take(3))
        Console.WriteLine($" [читач]  {r.Id,-6} {r.FullName,-24} {r.Email}");

    PrintErrors(m.Errors);
    Console.WriteLine(m.Stats);
    return 0;
}

// Вибір імпортера за розширенням файлу — знову switch expression.
Func<string, ImportResult<BookDto>>? importer = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => BookCsvImporter.Load,
    ".json" => BookJsonImporter.Load,
    _ => null
};

if (importer is null)
{
    Console.WriteLine($"Непідтримуване розширення '{Path.GetExtension(path)}': очікую .csv або .json");
    return 2;
}

ImportResult<BookDto> result = importer(path);

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
foreach (BookDto b in result.Items.Take(5))
    Console.WriteLine($" {b.Id,-6} {b.Isbn,-18} {b.Title,-30} {b.Year,5} {b.Author}");

PrintErrors(result.Errors);
Console.WriteLine(result.Stats);

return 0;

static void PrintErrors(IReadOnlyList<string> errors)
{
    if (errors.Count == 0)
        return;

    Console.WriteLine($"Пропущено рядків: {errors.Count}");
    foreach (string e in errors)
        Console.WriteLine($" ! {e}");
}