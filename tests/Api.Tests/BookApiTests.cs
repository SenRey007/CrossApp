using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Contracts;
using Core.Abstractions;
using Core.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests;

public class BookApiTests
{
    // Кожен тест отримує власний екземпляр API з порожніми сховищами: тести не залежать від SampleData і одне від одного.
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBookStore>();
                services.RemoveAll<IBookCopyStore>();
                services.AddSingleton<IBookStore>(new InMemoryBookStore());
                services.AddSingleton<IBookCopyStore>(new InMemoryBookCopyStore());
            }));

    private static SaveBookRequest Request(string isbn = "978-966-03-1234-5", string title = "Кобзар") =>
        new(isbn, title, "Тарас Шевченко", 1840);

    [Fact]
    public async Task Post_ValidBook_Returns201_AndGetByLocationReturns200()
    {
        using var factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/books", Request());

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Uri? location = created.Headers.Location;
        Assert.NotNull(location);

        HttpResponseMessage fetched = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

        BookResponse? book = await fetched.Content.ReadFromJsonAsync<BookResponse>();
        Assert.NotNull(book);
        Assert.Equal("Кобзар", book.Title);
        Assert.Equal(0, book.Copies);
    }

    [Fact]
    public async Task Post_InvalidBook_Returns400_WithFieldErrors()
    {
        using var factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsJsonAsync("/api/books", new SaveBookRequest("", "X", null, -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Isbn", out _));
        Assert.True(errors.TryGetProperty("Year", out _));
    }

    [Fact]
    public async Task Post_DuplicateIsbn_Returns409_WithProblemDetails()
    {
        using var factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/books", Request());

        HttpResponseMessage second = await client.PostAsJsonAsync("/api/books", Request(title: "Інша назва"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        JsonElement problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.Contains("ISBN", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        using var factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/books/no-such-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingBook_Returns204_ThenGetReturns404()
    {
        using var factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage created = await client.PostAsJsonAsync("/api/books", Request());
        Uri? location = created.Headers.Location;
        Assert.NotNull(location);

        HttpResponseMessage deleted = await client.DeleteAsync(location);
        HttpResponseMessage after = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task List_WithPaging_ReturnsRequestedPageAndTotal()
    {
        using var factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        for (int i = 1; i <= 5; i++)
            await client.PostAsJsonAsync("/api/books", Request(isbn: $"97896603{i:D5}", title: $"Книга {i}"));

        PagedResponse<BookResponse>? page =
            await client.GetFromJsonAsync<PagedResponse<BookResponse>>("/api/books?page=2&pageSize=2");

        Assert.NotNull(page);
        Assert.Equal(5, page.Total);
        Assert.Equal(2, page.Page);
        Assert.Equal(new[] { "Книга 3", "Книга 4" }, page.Items.Select(b => b.Title));
    }

    [Fact]
    public async Task List_WithInvalidPageSize_Returns400()
    {
        using var factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/books?pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("pageSize", out _));
    }
}