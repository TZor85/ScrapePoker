using OpenScrape.Domain.Enums;

namespace OpenScrape.Domain.ValueObjects;

/// <summary>
/// Clave tipada para lookup de <see cref="StreetThresholds"/>.
/// Sustituye la clave string "{Street}_{Situation}" que antes se consumía por
/// <c>Dictionary&lt;string, StreetThresholds&gt;</c> con fallback silencioso.
/// </summary>
public sealed record ThresholdKey
{
    public BoardPosition Street { get; }
    public HandSituation Situation { get; }

    public ThresholdKey(BoardPosition street, HandSituation situation)
    {
        if (street is BoardPosition.None or BoardPosition.Hand)
            throw new ArgumentException(
                $"BoardPosition '{street}' no es válido para un ThresholdKey postflop. Esperado: Flop, Turn o River.",
                nameof(street));

        if (situation == HandSituation.None)
            throw new ArgumentException(
                $"HandSituation '{situation}' no es válido para un ThresholdKey.",
                nameof(situation));

        Street = street;
        Situation = situation;
    }

    public override string ToString() => $"{Street}_{Situation}";

    /// <summary>
    /// Intenta parsear una clave string con formato <c>"{BoardPosition}_{HandSituation}"</c>.
    /// </summary>
    public static bool TryParse(string? rawKey, out ThresholdKey? key, out string error)
    {
        key = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            error = "La clave está vacía.";
            return false;
        }

        var parts = rawKey.Split('_', 2);
        if (parts.Length != 2)
        {
            error = "Formato inválido. Esperado '{Street}_{Situation}'.";
            return false;
        }

        if (!Enum.TryParse<BoardPosition>(parts[0], ignoreCase: false, out var street))
        {
            error = $"'{parts[0]}' no es un valor válido de BoardPosition.";
            return false;
        }

        if (!Enum.TryParse<HandSituation>(parts[1], ignoreCase: false, out var situation))
        {
            error = $"'{parts[1]}' no es un valor válido de HandSituation.";
            return false;
        }

        try
        {
            key = new ThresholdKey(street, situation);
            return true;
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
