using System.Runtime.CompilerServices;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class AsyncExtraTests
{
    private static IReadOnlyList<BookCopy> Copies(int count) =>
        Enumerable.Range(1, count).Select(i => BookCopy.Create($"C-{i}", "978-966-03-1234-5")).ToList();

    [Fact]
    public async Task ImportStreamAsync_AddsAll_AndRaisesOneEventPerCopy()
    {
        var store = new InMemoryBookCopyStore();
        var service = new LendingService(store);
        int events = 0;
        service.Changed += (_, _) => events++;

        int added = await service.ImportStreamAsync(new StubStreamImporter(Copies(3)), "ignored.json");

        Assert.Equal(3, added);
        Assert.Equal(3, events);
        Assert.Equal(3, store.List().Count);
    }

    [Fact]
    public async Task ImportStreamAsync_WhenCancelledMidStream_KeepsAlreadyAddedCopies()
    {
        var store = new InMemoryBookCopyStore();
        var service = new LendingService(store);
        using var cts = new CancellationTokenSource();
        int events = 0;
        service.Changed += (_, _) =>
        {
            events++;
            if (events == 2)
                cts.Cancel(); // скасування після другого запису
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportStreamAsync(new StubStreamImporter(Copies(5)), "ignored.json", cts.Token));

        Assert.Equal(2, store.List().Count); // потік не атомарний: додане лишається
    }

    [Fact]
    public async Task StreamAsync_WithInvalidRecord_YieldsValidOnesFirst_ThenThrowsDomainException()
    {
        string path = Path.Combine(Path.GetTempPath(), $"copies-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, """
            [
              { "id": "T-1", "isbn": "978-966-03-1234-5", "isIssued": false },
              { "id": "T-2", "isbn": "", "isIssued": false }
            ]
            """);
        try
        {
            var seen = new List<string>();

            var ex = await Assert.ThrowsAsync<DomainException>(async () =>
            {
                await foreach (BookCopy copy in new JsonCopyStreamImporter().StreamAsync(path))
                    seen.Add(copy.Id);
            });

            Assert.Equal(new[] { "T-1" }, seen);
            Assert.Contains("Запис 2", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ImportAsync_ReportsPercentProgress_UpTo100()
    {
        var service = new LendingService(new InMemoryBookCopyStore());
        var progress = new ListProgress();

        await service.ImportAsync(new StubListImporter(Copies(4)), "ignored.json", progress: progress);

        Assert.Equal(new[] { 25, 50, 75, 100 }, progress.Values);
    }

    [Fact]
    public async Task ImportWithTimeoutAsync_WhenImporterHangs_ThrowsTimeoutException()
    {
        var service = new LendingService(new InMemoryBookCopyStore());
        using var user = new CancellationTokenSource(); // користувач НЕ скасовує

        await Assert.ThrowsAsync<TimeoutException>(
            () => service.ImportWithTimeoutAsync(
                new HangingImporter(), "any.json", TimeSpan.FromMilliseconds(50), user.Token));

        Assert.False(user.IsCancellationRequested); // спрацював саме таймаут
    }

    [Fact]
    public async Task ImportWithTimeoutAsync_WhenUserCancels_IsNotReportedAsTimeout()
    {
        var service = new LendingService(new InMemoryBookCopyStore());
        using var user = new CancellationTokenSource();
        user.CancelAfter(TimeSpan.FromMilliseconds(50));

        Exception? ex = await Record.ExceptionAsync(
            () => service.ImportWithTimeoutAsync(
                new HangingImporter(), "any.json", TimeSpan.FromSeconds(30), user.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
        Assert.IsNotType<TimeoutException>(ex);
    }

    // ---------- підставні залежності: ні диска, ні консолі ----------

    private sealed class StubListImporter(IReadOnlyList<BookCopy> items) : ICopyImporter
    {
        public Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default) =>
            Task.FromResult(items);
    }

    private sealed class StubStreamImporter(IReadOnlyList<BookCopy> items) : ICopyStreamImporter
    {
        public async IAsyncEnumerable<BookCopy> StreamAsync(
            string path, [EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (BookCopy item in items)
            {
                await Task.Yield();
                yield return item;
            }
        }
    }

    private sealed class HangingImporter : ICopyImporter
    {
        public async Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
        {
            await Task.Delay(Timeout.Infinite, ct); // ніколи не завершується сам
            return [];
        }
    }

    private sealed class ListProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];
        public void Report(int value) => Values.Add(value);
    }
}