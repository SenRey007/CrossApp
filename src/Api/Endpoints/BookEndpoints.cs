using System.Security.Cryptography;
using Api.Contracts;
using Core.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class BookEndpoints
{
    public static void MapBooks(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/books").WithTags("Books");

        // 1) список, необов'язковий фільтр ?author=
        group.MapGet("/", (string? author, IBookStore books, IBookCopyStore copies) =>
            TypedResults.Ok(books.List()
                .Where(b => author is null
                            || (b.Author?.Contains(author, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(b => b.ToResponse(copies))
                .ToList()));

        // 2) одна книга за id
        group.MapGet("/{id}", Results<Ok<BookResponse>, NotFound>
            (string id, IBookStore books, IBookCopyStore copies) =>
            books.GetById(id) is { } found
                ? TypedResults.Ok(found.ToResponse(copies))
                : TypedResults.NotFound());

        // 3) створення: 201 + Location
        group.MapPost("/", Results<Created<BookResponse>, ValidationProblem>
            (SaveBookRequest request, IBookStore books, IBookCopyStore copies) =>
        {
            if (!request.TryValidate(out Dictionary<string, string[]> errors))
                return TypedResults.ValidationProblem(errors);

            try
            {
                Book created = Book.Create(NewId(), request.Isbn, request.Title, request.Year, request.Author);
                books.Add(created);
                return TypedResults.Created($"/api/books/{created.Id}", created.ToResponse(copies));
            }
            catch (ArgumentException ex) // правило домену, яке не перевіряє DTO (наприклад, формат ISBN)
            {
                return TypedResults.ValidationProblem(ToErrors(ex));
            }
        });

        // 4) повна заміна за id
        group.MapPut("/{id}", Results<Ok<BookResponse>, NotFound, ValidationProblem>
            (string id, SaveBookRequest request, IBookStore books, IBookCopyStore copies) =>
        {
            if (!request.TryValidate(out Dictionary<string, string[]> errors))
                return TypedResults.ValidationProblem(errors);
            if (books.GetById(id) is null)
                return TypedResults.NotFound();

            try
            {
                // Book незмінна: створюємо нову валідну сутність із тим самим id.
                Book replacement = Book.Create(id, request.Isbn, request.Title, request.Year, request.Author);
                books.Update(replacement);
                return TypedResults.Ok(replacement.ToResponse(copies));
            }
            catch (ArgumentException ex)
            {
                return TypedResults.ValidationProblem(ToErrors(ex));
            }
        });

        // 5) видалення
        group.MapDelete("/{id}", Results<NoContent, NotFound> (string id, IBookStore books) =>
            books.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound());
    }

    private static string NewId() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();

    // ArgumentException з домену -> словник «поле: повідомлення» для ValidationProblem.
    private static Dictionary<string, string[]> ToErrors(ArgumentException ex)
    {
        string key = ex.ParamName is { Length: > 0 } name ? char.ToUpperInvariant(name[0]) + name[1..] : "Request";
        string message = ex.Message.Split('\n')[0]
            .Replace($" (Parameter '{ex.ParamName}')", "")
            .TrimEnd();
        return new Dictionary<string, string[]> { [key] = [message] };
    }
}