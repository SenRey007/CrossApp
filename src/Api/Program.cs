using System.Text.Encodings.Web;
using Api.Endpoints;
using Core;
using Core.Abstractions;
using Core.Storage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Кирилиця у відповідях літерами, а не \uXXXX (як і в лабі 9; це API, а не HTML).
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(); // формат помилок RFC 9457

// Singleton: сховище в пам'яті має жити весь час роботи програми (Scoped втратив би дані після кожного запиту).
builder.Services.AddSingleton<IBookStore>(new InMemoryBookStore(SampleData.Books()));
builder.Services.AddSingleton<IBookCopyStore>(new InMemoryBookCopyStore(SampleData.ReportCopies()));
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();

WebApplication app = builder.Build();

app.UseExceptionHandler();   // неочікуваний виняток -> 500 ProblemDetails без stack trace
app.UseStatusCodePages();    // 404/415 тощо теж у форматі ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // /openapi/v1.json
    app.MapScalarApiReference();   // /scalar/v1
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapBooks();

app.Run();