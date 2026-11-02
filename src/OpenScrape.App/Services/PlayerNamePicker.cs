namespace OpenScrape.App.Services;

/// <summary>
/// Elige entre las dos lecturas de un nombre de jugador (umbral normal y umbral bajo).
/// La medición con Tesseract sobre capturas reales mostró que el umbral bajo lee los
/// cinco asientos, mientras el alto devuelve un punto en cuatro de ellos, así que ante
/// lecturas distintas gana la del umbral bajo.
/// </summary>
internal static class PlayerNamePicker
{
    public static string Choose(string? highThresholdRead, string? lowThresholdRead)
    {
        var high = highThresholdRead?.Trim() ?? string.Empty;
        var low = lowThresholdRead?.Trim() ?? string.Empty;

        if (high.Length == 0)
            return low;

        if (low.Length == 0)
            return high;

        if (string.Equals(high, low, StringComparison.Ordinal))
            return high;

        return low;
    }
}
