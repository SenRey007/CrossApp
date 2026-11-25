using Core.Abstractions;
using Core.Domain;

namespace Api.Contracts;

public sealed record BookResponse(string Id, string Isbn, string Title, string? Author, int Year, int Copies);

public static class BookMapping
{
    // Copies тут це кількість примірників цього видання у сховищі примірників.
    public static BookResponse ToResponse(this Book book, IBookCopyStore copies) => new(
        book.Id, book.Isbn, book.Title, book.Author, book.Year,
        copies.Find(c => string.Equals(c.Isbn, book.Isbn, StringComparison.OrdinalIgnoreCase)).Count);
}