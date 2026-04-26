using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Replaya manos históricas a través del motor de decisión actual y compara
/// con las decisiones originales almacenadas. Genera informe de divergencias
/// y estimación de impacto en BB/100.
/// </summary>
public class StrategyBacktester(IPostflopDecisionService decisionService) : Interfaces.IStrategyBacktester
{
    /// <summary>
    /// Ejecuta el backtest sobre un conjunto de manos históricas.
    /// Compara la decisión original (almacenada) con la que el motor actual produciría.
    /// </summary>
    public BacktestResult RunBacktest(List<HandRecord> hands, decimal bigBlind = 0.50m)
    {
        var result = new BacktestResult { TotalHands = hands.Count, BigBlind = bigBlind };

        foreach (var hand in hands)
        {
            foreach (var decision in hand.Decisions)
            {
                if (decision.Street == BoardPosition.Hand)
                    continue; // Skip preflop (no se replaya por PostflopDecisionService)

                var replay = ReplayDecision(decision, hand);
                if (replay == null) continue;

                result.TotalDecisions++;

                bool changed = !ActionsEquivalent(decision.RecommendedAction, replay.Action);
                if (changed)
                {
                    result.ChangedDecisions++;
                    result.Divergences.Add(new DecisionDivergence
                    {
                        HandNumber = hand.HandNumber,
                        Street = decision.Street,
                        Equity = decision.EquityPercent,
                        SPR = decision.SPR,
                        Situation = decision.Situation,
                        IsInPosition = decision.IsInPosition,
                        OriginalAction = decision.RecommendedAction,
                        NewAction = replay.Action,
                        NewReason = replay.Reason ?? "",
                        BoardTexture = decision.BoardTexture ?? "Unknown"
                    });

                    // Estimar impacto: cambios de Fold→Call/Bet o Call→Fold son los más significativos
                    result.EstimatedBBImpact += EstimateImpact(
                        decision.RecommendedAction, replay.Action,
                        decision.EquityPercent, decision.PotSizeAtDecision, bigBlind);
                }
            }
        }

        // Calcular BB/100 estimado
        if (result.TotalHands > 0)
            result.EstimatedBBPer100Impact = result.EstimatedBBImpact / result.TotalHands * 100;

        // Agrupar divergencias por tipo de cambio
        foreach (var div in result.Divergences)
        {
            var key = $"{Simplify(div.OriginalAction)} → {Simplify(div.NewAction)}";
            result.ChangesByType.TryGetValue(key, out int count);
            result.ChangesByType[key] = count + 1;
        }

        // Agrupar por street
        foreach (var div in result.Divergences)
        {
            result.ChangesByStreet.TryGetValue(div.Street, out int count);
            result.ChangesByStreet[div.Street] = count + 1;
        }

        return result;
    }

    /// <summary>
    /// Replaya una decisión individual usando el motor actual.
    /// </summary>
    private PostflopDecisionResult? ReplayDecision(StreetDecision original, HandRecord hand)
    {
        try
        {
            return decisionService.DetermineAction(new PostflopDecisionInput
            {
                Equity = original.EquityPercent,
                Street = original.Street,
                Situation = original.Situation,
                BoardTexture = original.BoardTexture ?? "Dry",
                IsInPosition = original.IsInPosition,
                VillainBetSize = original.BetSize > 0
                    ? CategorizeBet(original.BetSize, original.PotSizeAtDecision)
                    : BetSizeCategory.NoBet,
                PotOdds = original.PotOddsPercent,
                TotalOuts = original.TotalOuts,
                HeroStack = hand.HeroStackStart,
                PotSize = original.PotSizeAtDecision,
                NumOpponents = Math.Max(1, hand.NumOpponents),
            });
        }
        catch
        {
            return null; // Skip si falta algún dato
        }
    }

    private static BetSizeCategory CategorizeBet(decimal betSize, decimal potSize)
    {
        if (betSize <= 0) return BetSizeCategory.NoBet;
        if (potSize <= 0) return BetSizeCategory.Medium;
        var ratio = betSize / potSize;
        if (ratio <= 0.15m) return BetSizeCategory.Underbet;
        if (ratio <= 0.3m) return BetSizeCategory.Small;
        if (ratio <= 0.7m) return BetSizeCategory.Medium;
        return BetSizeCategory.Large;
    }

    /// <summary>
    /// Compara acciones simplificadas (ignora sizing, solo compara tipo: Bet/Call/Raise/Check/Fold/All-In).
    /// </summary>
    private static bool ActionsEquivalent(string action1, string action2)
    {
        return Simplify(action1) == Simplify(action2);
    }

    private static string Simplify(string action)
    {
        if (string.IsNullOrEmpty(action)) return "Check";
        var lower = action.ToLowerInvariant();
        if (lower.Contains("all-in")) return "All-In";
        if (lower.Contains("raise")) return "Raise";
        if (lower.Contains("fold")) return "Fold";
        if (lower.Contains("call")) return "Call";
        if (lower.Contains("bet") || lower.Contains("c-bet") || lower.Contains("barrel") || lower.Contains("value")) return "Bet";
        if (lower.Contains("check")) return "Check";
        return "Check";
    }

    /// <summary>
    /// Estima el impacto en BB de un cambio de decisión.
    /// </summary>
    private static double EstimateImpact(string oldAction, string newAction,
        double equity, decimal potSize, decimal bigBlind)
    {
        if (bigBlind <= 0) return 0;

        string oldSimple = Simplify(oldAction);
        string newSimple = Simplify(newAction);
        double potBB = (double)(potSize / bigBlind);

        // Fold → Bet/Call: ganamos equity del pot que antes perdíamos
        if (oldSimple == "Fold" && newSimple is "Call" or "Bet")
            return (equity / 100.0) * potBB * 0.5; // Conservador: 50% del EV teórico

        // Bet/Call → Fold: ahorramos bet cuando equity es baja
        if (oldSimple is "Call" or "Bet" && newSimple is "Fold" or "Check")
            return (1.0 - equity / 100.0) * potBB * 0.3; // Conservador: ahorro parcial

        // Check → Bet: capturamos valor extra
        if (oldSimple == "Check" && newSimple == "Bet")
            return (equity / 100.0) * potBB * 0.2; // Modesto: no siempre paga

        // Bet → Check: evitamos bet malo
        if (oldSimple == "Bet" && newSimple == "Check")
            return (1.0 - equity / 100.0) * potBB * 0.15;

        return 0;
    }
}

/// <summary>
/// Resultado del backtest A/B.
/// </summary>
public class BacktestResult
{
    public int TotalHands { get; set; }
    public int TotalDecisions { get; set; }
    public int ChangedDecisions { get; set; }
    public decimal BigBlind { get; set; }

    /// <summary>Porcentaje de decisiones que cambian con el motor nuevo.</summary>
    public double ChangeRate => TotalDecisions > 0 ? (double)ChangedDecisions / TotalDecisions * 100 : 0;

    /// <summary>Impacto estimado total en BB.</summary>
    public double EstimatedBBImpact { get; set; }

    /// <summary>Impacto estimado en BB/100 manos.</summary>
    public double EstimatedBBPer100Impact { get; set; }

    /// <summary>Cambios agrupados por tipo (ej: "Fold → Call": 15).</summary>
    public Dictionary<string, int> ChangesByType { get; set; } = new();

    /// <summary>Cambios agrupados por street.</summary>
    public Dictionary<BoardPosition, int> ChangesByStreet { get; set; } = new();

    /// <summary>Lista detallada de divergencias.</summary>
    public List<DecisionDivergence> Divergences { get; set; } = new();

    public override string ToString()
    {
        var lines = new List<string>
        {
            $"═══ BACKTEST RESULT ═══",
            $"Manos analizadas: {TotalHands}",
            $"Decisiones evaluadas: {TotalDecisions}",
            $"Decisiones que cambian: {ChangedDecisions} ({ChangeRate:F1}%)",
            $"Impacto estimado: {EstimatedBBPer100Impact:+0.0;-0.0} BB/100",
            $"",
            $"─── Por tipo de cambio ───"
        };

        foreach (var (key, count) in ChangesByType.OrderByDescending(x => x.Value))
            lines.Add($"  {key}: {count}");

        lines.Add($"");
        lines.Add($"─── Por street ───");
        foreach (var (street, count) in ChangesByStreet.OrderByDescending(x => x.Value))
            lines.Add($"  {street}: {count}");

        return string.Join(Environment.NewLine, lines);
    }
}

public class DecisionDivergence
{
    public long HandNumber { get; set; }
    public BoardPosition Street { get; set; }
    public double Equity { get; set; }
    public double SPR { get; set; }
    public HandSituation Situation { get; set; }
    public bool IsInPosition { get; set; }
    public string OriginalAction { get; set; } = "";
    public string NewAction { get; set; } = "";
    public string NewReason { get; set; } = "";
    public string BoardTexture { get; set; } = "";
}
