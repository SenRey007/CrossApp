using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new LendingService(new InMemoryBookCopyStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCopyImporter(), "any.json", cts.Token));
    }

    [Fact]
    public async Task LoadAsync_WhenAlreadyCancelled_ThrowsBeforeOpeningFile()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new JsonCopyImporter().LoadAsync("does-not-exist.json", cts.Token));
    }

    [Fact]
    public async Task LoadAsync_WithInvalidRecord_ThrowsDomainException_WithRecordNumber()
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
            var ex = await Assert.ThrowsAsync<DomainException>(
                () => new JsonCopyImporter().LoadAsync(path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.IsType<ArgumentException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }
}