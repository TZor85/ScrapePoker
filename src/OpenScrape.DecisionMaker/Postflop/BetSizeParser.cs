namespace OpenScrape.DecisionMaker.Postflop;

/// <summary>
/// Traduce el texto de una apuesta ("Bet Pot", "Bet 1/2") a la fracción del
/// pote que representa. El servicio postflop codifica el tamaño dentro de la
/// acción; esto permite exponerlo como número sin tocar la decisión.
/// </summary>
public static class BetSizeParser
{
    /// <summary>
    /// Fracción del pote que representa un texto de tamaño. Devuelve null
    /// cuando el texto no declara un tamaño conocido.
    /// </summary>
    public static double? FromSizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (text.Contains("Pot", StringComparison.OrdinalIgnoreCase))
            return 1.0;

        if (text.Contains("3/4"))
            return 0.75;

        if (text.Contains("2/3"))
            return 0.67;

        if (text.Contains("1/2"))
            return 0.50;

        if (text.Contains("1/3"))
            return 0.33;

        if (text.Contains("1/4"))
            return 0.25;

        if (text.Contains("1.25x", StringComparison.OrdinalIgnoreCase))
            return 1.25;

        return null;
    }

    /// <summary>
    /// Fracción del pote de una acción con tamaño ("Bet Pot (Value)",
    /// "Raise 1/2"). Devuelve null para acciones sin tamaño (Check, Call, Fold).
    /// </summary>
    public static double? FromAction(string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return null;

        var declaresBet = action.Contains("Bet", StringComparison.OrdinalIgnoreCase)
            || action.Contains("Raise", StringComparison.OrdinalIgnoreCase);

        return declaresBet ? FromSizeText(action) : null;
    }
}
