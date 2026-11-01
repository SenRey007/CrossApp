using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class LendingServiceTests
{
    private static (LendingService Service, InMemoryBookCopyStore Store) Create()
    {
        var store = new InMemoryBookCopyStore();
        return (new LendingService(store), store);
    }

    [Fact]
    public void AddBook_RaisesChangedExactlyOnce()
    {
        var (service, _) = Create();
        var events = new List<LendingChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        BookCopy copy = service.AddBook("978-966-03-1234-5");

        Assert.Single(events);
        Assert.Equal(LendingChangeKind.BookAdded, events[0].Kind);
        Assert.Equal(copy.Id, events[0].CopyId);
    }

    [Fact]
    public void IssueCopy_RaisesCopyIssued_WithReaderAndDueDate()
    {
        var (service, _) = Create();
        BookCopy copy = service.AddBook("978-966-03-1234-5");
        var events = new List<LendingChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.IssueCopy("L-1", copy.Id, "R-1", new DateOnly(2026, 10, 1));

        LendingChangedEventArgs issued = Assert.Single(events);
        Assert.Equal(LendingChangeKind.CopyIssued, issued.Kind);
        Assert.Equal("R-1", issued.ReaderId);
        Assert.Equal<DateOnly?>(new DateOnly(2026, 10, 15), issued.DueDate);
    }

    [Fact]
    public void IssueCopy_WhenReaderLimitReached_ThrowsDomainException_AndRaisesNoEvent()
    {
        var (service, _) = Create();
        var day = new DateOnly(2026, 10, 1);

        var copies = new List<BookCopy>();
        for (int i = 0; i <= LendingService.MaxOpenLoansPerReader; i++)
            copies.Add(service.AddBook("978-966-03-1234-5")); // на один більше за ліміт

        for (int i = 0; i < LendingService.MaxOpenLoansPerReader; i++)
            service.IssueCopy($"L-{i}", copies[i].Id, "R-1", day);

        int events = 0;
        service.Changed += (_, _) => events++;

        BookCopy extra = copies[^1];
        Assert.Throws<DomainException>(() => service.IssueCopy("L-X", extra.Id, "R-1", day));

        Assert.Equal(0, events);
        Assert.False(extra.IsIssued);
    }

    [Fact]
    public async Task ImportAsync_IsIdempotent_AndRaisesOneEventPerNewCopy()
    {
        var (service, store) = Create();
        var importer = new FakeImporter(
        [
            BookCopy.Create("C-1", "978-966-03-1234-5"),
            BookCopy.Create("C-2", "978-617-12-5678-9"),
        ]);
        int events = 0;
        service.Changed += (_, _) => events++;

        int first = await service.ImportAsync(importer, "ignored.json");
        int second = await service.ImportAsync(importer, "ignored.json");

        Assert.Equal(2, first);
        Assert.Equal(0, second);
        Assert.Equal(2, events);
        Assert.Equal(2, store.List().Count);
    }

    // Підставний імпортер: ні диска, ні файлу.
    private sealed class FakeImporter(IReadOnlyList<BookCopy> items) : ICopyImporter
    {
        public Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default) =>
            Task.FromResult(items);
    }
}