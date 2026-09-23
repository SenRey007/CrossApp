using System.Globalization;

namespace Core.Dto;

/// <summary>Статистика імпорту: заготовка під звіти тижня 7.</summary>
public sealed record ImportStats(int Total, int Accepted, int Skipped)
{
    public double ErrorPercent => Total == 0 ? 0 : 100.0 * Skipped / Total;

    public static ImportStats Create(int accepted, int skipped)
        => new(accepted + skipped, accepted, skipped);

    // Явна InvariantCulture: "12.5%", а не "12,5%" залежно від локалі ОС.
    public override string ToString() => string.Create(CultureInfo.InvariantCulture,
        $"Усього: {Total}, прийнято: {Accepted}, пропущено: {Skipped} ({ErrorPercent:F1}% помилок)");
}
