namespace Core.Dto;

public sealed record ImportResult<T>(IReadOnlyList<T> Items, IReadOnlyList<string> Errors)
{
    public ImportStats Stats => ImportStats.Create(Items.Count, Errors.Count);
}