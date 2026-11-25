namespace Api.Contracts;

public sealed record SaveBookRequest(string Isbn, string Title, string? Author, int Year)
{
    public bool TryValidate(out Dictionary<string, string[]> errors)
    {
        var found = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(Isbn))
            found[nameof(Isbn)] = ["ISBN обов'язковий."];
        else if (Isbn.Length > 20)
            found[nameof(Isbn)] = ["ISBN занадто довгий (до 20 символів)."];

        if (string.IsNullOrWhiteSpace(Title) || Title.Length > 200)
            found[nameof(Title)] = ["Назва обов'язкова, до 200 символів."];

        if (Author is { Length: > 100 })
            found[nameof(Author)] = ["Автор: до 100 символів."];

        if (Year < 1450 || Year > DateTime.Now.Year)
            found[nameof(Year)] = [$"Рік має бути в межах 1450..{DateTime.Now.Year}."];

        errors = found;
        return found.Count == 0;
    }
}