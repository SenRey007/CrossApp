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
    public static AppSettings Load(string[] args) 
    {
        string? fromEnv = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT");
        string profile = string.IsNullOrWhiteSpace(fromEnv) ? "Development" : fromEnv;

        // Порядок важливий: що пізніше додано, те перекриває попереднє.
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{profile}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_");

        // Проста змінна CROSSAPP_DATA_PATH: вища за файли і CROSSAPP_Storage__DataPath, нижча за аргументи.
        string? simple = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        if (!string.IsNullOrWhiteSpace(simple))
            builder.AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:DataPath"] = simple });

        IConfiguration config = builder.AddCommandLine(args).Build();

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
}