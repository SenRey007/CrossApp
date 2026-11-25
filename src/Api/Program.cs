using System.Text.Encodings.Web;
using Api.Contracts;
using Api.Endpoints;
using Api.Errors;
using Api.Validation;
using Core;
using Core.Abstractions;
using Core.Storage;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>(); // DomainException -> 409; решта -> 500

builder.Services.AddSingleton<IValidator<SaveBookRequest>, SaveBookRequestValidator>();
builder.Services.AddSingleton<IBookStore>(new InMemoryBookStore(SampleData.Books()));
builder.Services.AddSingleton<IBookCopyStore>(new InMemoryBookCopyStore(SampleData.ReportCopies()));

WebApplication app = builder.Build();


app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (BadHttpRequestException)
    {
        if (context.Response.HasStarted)
            throw;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            title = "Некоректний формат запиту або JSON.",
            status = 400
        });
    }
});
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapBooks();

app.Run();

// Робить тип Program доступним для WebApplicationFactory<Program> в інтеграційних тестах.
public partial class Program { }