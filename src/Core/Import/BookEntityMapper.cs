using Core.Domain;
using Core.Dto;

namespace Core.Import;

public static class BookEntityMapper
{
    // Та сама ідея «дані + помилки», що й в імпорті: один поганий запис не зупиняє решту.
    public static ImportResult<Book> ToEntities(ImportResult<BookDto> imported)
    {
        var books = new List<Book>();
        var errors = new List<string>(imported.Errors); // помилки імпорту переносимо

        foreach (BookDto dto in imported.Items)
        {
            try
            {
                books.Add(Book.FromDto(dto));
            }
            catch (ArgumentException ex) // ловить і ArgumentOutOfRangeException
            {
                errors.Add($"книга {dto.Id}: {ex.Message.ReplaceLineEndings(" ")}");
            }
        }

        return new ImportResult<Book>(books, errors);
    }
}