using System.Globalization;

using OpenScrape.App.Entities;

namespace OpenScrape.App.Services;

/// <summary>
/// Implementación pura del <see cref="IActionFormatter"/>. Sin estado.
/// Preserva comportamiento 1:1 de <c>FrmMain.EnrichActionWithBBAmount</c>.
/// </summary>
public sealed class ActionFormatter : IActionFormatter
{
    private const decimal DefaultBigBlindFallback = 0.50m;

    public string EnrichActionWithBBAmount(string action, IEnumerable<Player> villains, decimal bigBlind)
    {
        if (string.IsNullOrEmpty(action) || !action.Contains('x'))
            return action;

        var xIndex = action.LastIndexOf('x');
        if (xIndex < 0 || xIndex >= action.Length - 1)
            return action;

        var multiplierStr = action[(xIndex + 1)..].Trim();
        if (!double.TryParse(multiplierStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var multiplier)
            || multiplier <= 0)
        {
            return action;
        }

        if (bigBlind <= 0) bigBlind = DefaultBigBlindFallback;

        decimal maxVillainBet = 0;
        foreach (var v in villains)
        {
            if (v.Bet > maxVillainBet)
                maxVillainBet = v.Bet;
        }

        decimal baseBet = maxVillainBet > 0 ? maxVillainBet : bigBlind;
        decimal totalBet = baseBet * (decimal)multiplier;
        decimal totalBB = bigBlind > 0 ? Math.Round(totalBet / bigBlind, 1) : 0;

        if (totalBB > 0)
            return $"{action} ({totalBB}BB)";

        return action;
    }
}
