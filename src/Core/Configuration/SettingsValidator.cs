namespace Core.Configuration;

public static class SettingsValidator
{
    private static readonly string[] Levels = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];

    public static IReadOnlyList<string> Validate(StorageOptions storage, LibraryOptions library, LoggingOptions logging)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(storage.DataPath))
            errors.Add("Storage:DataPath не може бути порожнім.");

        if (string.IsNullOrWhiteSpace(storage.FileName))
            errors.Add("Storage:FileName не може бути порожнім.");
        else if (!storage.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            errors.Add($"Storage:FileName = '{storage.FileName}' має закінчуватися на .json (файл сховища завжди JSON).");

        string format = (storage.Format ?? "").Trim().ToLowerInvariant();
        if (format is not ("json" or "csv"))
            errors.Add($"Storage:Format = '{storage.Format}' не підтримується: очікую json або csv.");
        else if (string.IsNullOrWhiteSpace(storage.BooksFileName))
            errors.Add("Storage:BooksFileName не може бути порожнім.");
        else if (!storage.BooksFileName.EndsWith("." + format, StringComparison.OrdinalIgnoreCase))
            errors.Add($"Storage:BooksFileName = '{storage.BooksFileName}' має закінчуватися на .{format} (формат {format}).");

        if (library.LoanDays <= 0)
            errors.Add($"Library:LoanDays має бути більшим за нуль, а зараз {library.LoanDays}.");

        if (!Levels.Contains(logging.MinimumLevel, StringComparer.OrdinalIgnoreCase))
            errors.Add($"Logging:MinimumLevel = '{logging.MinimumLevel}' не підтримується: очікую {string.Join(", ", Levels)}.");

        return errors;
    }
}