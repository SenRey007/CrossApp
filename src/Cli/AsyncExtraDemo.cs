using System.Runtime.CompilerServices;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class AsyncExtraDemo
{
    public static async Task RunAsync()
    {
        string path = Path.Combine("data", "copies.json");
        if (!File.Exists(path))
        {
            Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
            return;
        }

        await PartStreamAsync(path);
        Console.WriteLine();
        await PartLinkedAsync(path);
        Console.WriteLine();
        await PartProgressAsync(path);
    }

    private static async Task PartStreamAsync(string path)
    {
        Console.WriteLine("=== Додаткове 1: потоковий імпорт (IAsyncEnumerable) ===");

        var full = new LendingService(new InMemoryBookCopyStore());
        int fullEvents = 0;
        full.Changed += (_, _) => fullEvents++;
        int added = await full.ImportStreamAsync(new JsonCopyStreamImporter(), path);
        Console.WriteLine($" Повний потоковий імпорт: додано {added}, подій {fullEvents}");

        // Повільне джерело: 300 мс на запис і таймаут 1000 мс, скасування посеред потоку.
        var cut = new LendingService(new InMemoryBookCopyStore());
        int cutEvents = 0;
        cut.Changed += (_, _) => cutEvents++;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
        try
        {
            var paced = new PacedStreamImporter(new JsonCopyStreamImporter(), TimeSpan.FromMilliseconds(300));
            await cut.ImportStreamAsync(paced, path, cts.Token);
            Console.WriteLine(" Не скасовано?!");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($" Скасовано посеред потоку: до скасування додано {cutEvents} з 10");
        }
    }

    private static async Task PartLinkedAsync(string path)
    {
        Console.WriteLine("=== Додаткове 2: зв'язані токени (таймаут проти користувача) ===");
        var service = new LendingService(new InMemoryBookCopyStore());
        ICopyImporter slow = new DelayImporter(new JsonCopyImporter(), TimeSpan.FromSeconds(3));

        try
        {
            await service.ImportWithTimeoutAsync(slow, path, TimeSpan.FromMilliseconds(500));
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($" Таймаут: {ex.Message}");
        }

        using var user = new CancellationTokenSource(TimeSpan.FromMilliseconds(300)); // «користувач» натиснув Ctrl+C
        try
        {
            await service.ImportWithTimeoutAsync(slow, path, TimeSpan.FromSeconds(5), user.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(" Скасовано користувачем (не таймаут).");
        }

        int added = await service.ImportWithTimeoutAsync(new JsonCopyImporter(), path, TimeSpan.FromSeconds(5));
        Console.WriteLine($" Без скасування: додано {added}");
    }

    private static async Task PartProgressAsync(string path)
    {
        Console.WriteLine("=== Додаткове 3: IProgress<int> ===");
        var service = new LendingService(new InMemoryBookCopyStore());
        await service.ImportAsync(new JsonCopyImporter(), path, progress: new ConsoleProgress());
    }

    // Власна синхронна реалізація: Progress<T> у консолі викликає обробник у пулі потоків,
    // тому порядок і момент виводу не гарантовані.
    private sealed class ConsoleProgress : IProgress<int>
    {
        public void Report(int percent)
        {
            int filled = percent / 5;
            Console.WriteLine($" [{new string('#', filled)}{new string('-', 20 - filled)}] {percent,3}%");
        }
    }

    private sealed class PacedStreamImporter(ICopyStreamImporter inner, TimeSpan pace) : ICopyStreamImporter
    {
        public async IAsyncEnumerable<BookCopy> StreamAsync(
            string path, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (BookCopy copy in inner.StreamAsync(path).WithCancellation(ct))
            {
                await Task.Delay(pace, ct);
                yield return copy;
            }
        }
    }

    private sealed class DelayImporter(ICopyImporter inner, TimeSpan delay) : ICopyImporter
    {
        public async Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
        {
            await Task.Delay(delay, ct);
            return await inner.LoadAsync(path, ct);
        }
    }
}