using OpenScrape.Contracts;

namespace OpenScrape.App.Services;

/// <summary>Números que el resumen preflop muestra al usuario.</summary>
public readonly record struct PreflopStats(double Equity, double PotOdds, bool ShouldCall);

/// <summary>
/// Resuelve los números del resumen preflop. Cuando hay decisión del motor se usan
/// sus valores tal cual: si el cliente los recalcula por su cuenta (equity más un
/// ajuste por posición, pot odds desde la apuesta máxima) el resumen puede decir
/// "call" mientras la tabla dice fold, porque compara otros dos números.
/// </summary>
public static class PreflopStatsCalculator
{
    public static PreflopStats Resolve(DecisionResult? decision, decimal potSize, decimal maxBet)
    {
        if (decision is not null)
        {
            return new PreflopStats(
                decision.Equity,
                decision.PotOdds,
                decision.Equity >= decision.PotOdds);
        }

        var potOdds = (double)CalculatePotOddsPercentage(potSize, maxBet);

        return new PreflopStats(0d, potOdds, 0d >= potOdds);
    }

    internal static decimal CalculatePotOddsPercentage(decimal potSize, decimal betToCall)
    {
        if (betToCall <= 0)
            return 0m;

        var total = potSize + betToCall;

        return total <= 0 ? 0m : betToCall / total * 100m;
    }
}
