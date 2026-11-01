using System.Globalization;
using System.Text.Json;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class AsyncDemo
{
    public static async Task<int> RunAsync(string[] args)
    {
        string path = args.FirstOrDefault(a => !a.StartsWith("--")) ?? Path.Combine("data", "copies.json");
        bool slow = args.Contains("--slow");
        int? timeoutMs = ParseTimeout(args);

        IBookCopyStore store = StoreFactory.Create(args);
        var service = new LendingService(store);
        int events = 0;
        service.Changed += (_, _) => events++;
        Console.WriteLine($"Сховище: {store.GetType().Name}");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        if (timeoutMs is { } ms)
            cts.CancelAfter(TimeSpan.FromMilliseconds(ms));

        ICopyImporter importer = new JsonCopyImporter();
        if (slow)
            importer = new SlowImporter(importer, TimeSpan.FromSeconds(3));

        try
        {
            int added = await service.ImportAsync(importer, path, cts.Token);
            Console.WriteLine($"Імпортовано записів: {added} (подій: {events})");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Імпорт скасовано.");
            return 3;
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message}");
            return 4;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Файл має некоректний JSON: {ex.Message}");
            return 4;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
            return 1;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Не вдалося прочитати файл: {ex.Message}");
            return 1;
        }
    }

    // --timeout=1000 (мілісекунди), а не окремий аргумент, щоб значення не сприйняли за шлях.
    private static int? ParseTimeout(string[] args)
    {
        string? arg = args.FirstOrDefault(a => a.StartsWith("--timeout=", StringComparison.Ordinal));
        if (arg is null)
            return null;

        string value = arg["--timeout=".Length..];
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ms) && ms >= 0
            ? ms
            : null;
    }

    // Лише для демонстрації скасування: імітує повільне джерело (справжня async-затримка, а не Thread.Sleep).
    private sealed class SlowImporter(ICopyImporter inner, TimeSpan delay) : ICopyImporter
    {
        public async Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
        {
            Console.WriteLine($" (імітація повільного джерела: {delay.TotalSeconds:F0} с; Ctrl+C скасовує)");
            await Task.Delay(delay, ct);
            return await inner.LoadAsync(path, ct);
        }
    }
}