using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cli;

public sealed record AppSettings(
    string ProfileName,
    string EnvironmentName,
    StorageOptions Storage,
    LibraryOptions Library,
    LoggingOptions Logging,
    string DataFile,
    string BooksFile,
    string? ApiKey);

public static class AppConfig
{
    // Короткі псевдоніми: --data-path=... замість --Storage:DataPath=...
    private static readonly Dictionary<string, string> SwitchMappings = new()
    {
        ["--data-path"] = "Storage:DataPath",
        ["--format"] = "Storage:Format",
    };

    public static AppSettings Load(string[] args)
    {
        string? fromEnv = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT");
        string profile = string.IsNullOrWhiteSpace(fromEnv) ? "Development" : fromEnv;

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{profile}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_");

        string? simple = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        if (!string.IsNullOrWhiteSpace(simple))
            builder.AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:DataPath"] = simple });

        // До конфігурації передаємо лише аргументи виду --Ключ=значення. Прапорці без «=»
        // (--config, --export...) провайдер читав би як «--ключ значення» і «з'їв» би наступний аргумент.
        string[] overrides = args
            .Where(a => a.StartsWith("--", StringComparison.Ordinal) && a.Contains('='))
            .ToArray();

        IConfiguration config = builder.AddCommandLine(overrides, SwitchMappings).Build();
        AppOptions options = config.Get<AppOptions>() ?? new AppOptions();

        return new AppSettings(
            profile,
            options.EnvironmentName,
            options.Storage,
            options.Library,
            options.Logging,
            options.Storage.ResolveFullPath(AppContext.BaseDirectory),
            options.Storage.ResolveBooksPath(AppContext.BaseDirectory),
            config["Integration:ApiKey"]);
    }

    // Перевірка на старті: помилки повертаються списком, а не виняткомми.
    public static bool TryLoad(string[] args, out IReadOnlyList<string> errors)
    {
        try
        {
            AppSettings s = Load(args);
            errors = SettingsValidator.Validate(s.Storage, s.Library, s.Logging);
            return errors.Count == 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException
                                       or FileNotFoundException or InvalidDataException)
        {
            errors = [$"Не вдалося прочитати конфігурацію: {ex.Message}"];
            return false;
        }
    }
}