using System.Security.Cryptography;
using Api.Contracts;
using Api.Validation;
using Core.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class BookEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public static void MapBooks(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/books").WithTags("Books");

        group.MapGet("/", Results<Ok<PagedResponse<BookResponse>>, ValidationProblem>
            (string? author, string? search, string? page, string? pageSize, IBookStore books, IBookCopyStore copies) =>
        {
            var errors = new Dictionary<string, string[]>();

            int pageNumber = 1;
            if (!string.IsNullOrWhiteSpace(page))
            {
                if (!int.TryParse(page, out pageNumber) || pageNumber < 1)
                    errors["page"] = ["page має бути цілим числом не менше 1."];
            }

            int size = DefaultPageSize;
            if (!string.IsNullOrWhiteSpace(pageSize))
            {
                if (!int.TryParse(pageSize, out size) || size < 1 || size > MaxPageSize)
                    errors["pageSize"] = [$"pageSize має бути цілим числом у межах 1..{MaxPageSize}."];
            }

            if (errors.Count > 0)
                return TypedResults.ValidationProblem(errors);
            IEnumerable<Book> query = books.List();
            if (!string.IsNullOrWhiteSpace(author))
                query = query.Where(b => b.Author?.Contains(author, StringComparison.OrdinalIgnoreCase) ?? false);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => Matches(b, search));

            // Детермінований порядок (другий ключ id), щоб сторінки не «пливли» між запитами.
            List<Book> filtered = query
                .OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(b => b.Id, StringComparer.Ordinal)
                .ToList();

            int skip = (int)Math.Min((long)(pageNumber - 1) * size, filtered.Count); // без переповнення
            List<BookResponse> items = filtered.Skip(skip).Take(size).Select(b => b.ToResponse(copies)).ToList();

            return TypedResults.Ok(new PagedResponse<BookResponse>(items, filtered.Count, pageNumber, size));
        });

        // 2) одна книга за id
        group.MapGet("/{id}", Results<Ok<BookResponse>, NotFound>
            (string id, IBookStore books, IBookCopyStore copies) =>
            books.GetById(id) is { } found
                ? TypedResults.Ok(found.ToResponse(copies))
                : TypedResults.NotFound());

        // 3) створення: DTO перевіряє фільтр, дубль ISBN дає DomainException -> 409
        group.MapPost("/", Results<Created<BookResponse>, ValidationProblem>
            (SaveBookRequest request, IBookStore books, IBookCopyStore copies) =>
        {
            try
            {
                Book created = Book.Create(NewId(), request.Isbn, request.Title, request.Year, request.Author);
                books.Add(created);
                return TypedResults.Created($"/api/books/{created.Id}", created.ToResponse(copies));
            }
            catch (ArgumentException ex) // правило домену, якого не знає DTO (наприклад, формат ISBN)
            {
                return TypedResults.ValidationProblem(ToErrors(ex));
            }
        }).AddEndpointFilter<ValidationFilter<SaveBookRequest>>();

        // 4) повна заміна за id
        group.MapPut("/{id}", Results<Ok<BookResponse>, NotFound, ValidationProblem>
            (string id, SaveBookRequest request, IBookStore books, IBookCopyStore copies) =>
        {
            if (books.GetById(id) is null)
                return TypedResults.NotFound();

            try
            {
                Book replacement = Book.Create(id, request.Isbn, request.Title, request.Year, request.Author);
                books.Update(replacement); // ISBN іншої книги -> DomainException -> 409
                return TypedResults.Ok(replacement.ToResponse(copies));
            }
            catch (ArgumentException ex)
            {
                return TypedResults.ValidationProblem(ToErrors(ex));
            }
        }).AddEndpointFilter<ValidationFilter<SaveBookRequest>>();

        // 5) видалення
        group.MapDelete("/{id}", Results<NoContent, NotFound> (string id, IBookStore books) =>
            books.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound());
    }

    private static bool Matches(Book b, string search) =>
        b.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
        || (b.Author?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
        || b.Isbn.Contains(search, StringComparison.OrdinalIgnoreCase);

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