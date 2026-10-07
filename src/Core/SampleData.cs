using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;
namespace Core;

public static class SampleData
{
    public static IEnumerable<BookCopy> Copies() =>
    [
        BookCopy.Create("C-001", "978-966-03-1234-5"),
        BookCopy.Create("C-002", "978-617-12-5678-9"),
        BookCopy.Create("C-003", "978-966-97-1111-2"),
        BookCopy.Create("C-004", "978-966-97-2222-3"),
        BookCopy.Create("C-005", "978-617-12-3333-4"),
        BookCopy.Create("C-006", "978-966-03-4444-5"),
    ];
}