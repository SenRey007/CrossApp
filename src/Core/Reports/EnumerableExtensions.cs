namespace Core.Reports;

public static class EnumerableExtensions
{
    // Топ-N за числовим ключем. Другий ключ (необов'язковий) робить порядок при нічиїх детермінованим.
    public static IEnumerable<T> TopBy<T>(this IEnumerable<T> source, Func<T, int> key, int n,
        Func<T, string>? thenBy = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        IOrderedEnumerable<T> ordered = source.OrderByDescending(key);
        if (thenBy is not null)
            ordered = ordered.ThenBy(thenBy, StringComparer.Ordinal);

        return ordered.Take(n);
    }
}