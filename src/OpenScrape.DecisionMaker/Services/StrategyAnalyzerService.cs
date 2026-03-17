using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

// StrategyAnalyzerService trabaja con HandRecord (manos individuales)
// extraídas de GameSession.Hands

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Resultado del análisis de estrategia sobre un conjunto de manos.
/// </summary>
public class StrategyAnalysisResult
{
    // General
    public int TotalHands { get; set; }
    public int HandsWon { get; set; }
    public int HandsLost { get; set; }
    public int HandsPush { get; set; }
    public int HandsUnknown { get; set; }
    public double WinRate => TotalHands > 0 ? (double)HandsWon / TotalHands * 100 : 0;

    // Profit
    public decimal TotalProfit { get; set; }
    public decimal BiggestWin { get; set; }
    public decimal BiggestLoss { get; set; }

    // BB/100 (Big Blinds per 100 hands)
    public double BBPer100 { get; set; }

    // Por posición
    public Dictionary<TablePosition, PositionStats> StatsByPosition { get; set; } = new();

    // Por street
    public Dictionary<BoardPosition, StreetStats> StatsByStreet { get; set; } = new();

    // Por situación
    public Dictionary<HandSituation, SituationStats> StatsBySituation { get; set; } = new();

    // Por sesión
    public List<SessionSummary> Sessions { get; set; } = new();

    // Equity accuracy
    public double EquityAccuracy { get; set; }
    public List<EquityVsOutcome> EquityVsOutcomes { get; set; } = new();
}

public class PositionStats
{
    public TablePosition Position { get; set; }
    public int Hands { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
    public decimal Profit { get; set; }
    public double WinRate => Hands > 0 ? (double)Won / Hands * 100 : 0;
    public double AvgProfitPerHand => Hands > 0 ? (double)(Profit / Hands) : 0;
}

public class StreetStats
{
    public BoardPosition Street { get; set; }
    public int TotalDecisions { get; set; }
    public int Bets { get; set; }
    public int Calls { get; set; }
    public int Raises { get; set; }
    public int Folds { get; set; }
    public int Checks { get; set; }
    public double AvgEquity { get; set; }
    public double AvgPotOdds { get; set; }
}

public class SituationStats
{
    public HandSituation Situation { get; set; }
    public int Hands { get; set; }
    public int Won { get; set; }
    public decimal Profit { get; set; }
    public double WinRate => Hands > 0 ? (double)Won / Hands * 100 : 0;
}

public class SessionSummary
{
    public string SessionId { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Hands { get; set; }
    public decimal Profit { get; set; }
    public double BBPer100 { get; set; }
}

public class EquityVsOutcome
{
    public double Equity { get; set; }
    public bool Won { get; set; }
    public decimal Profit { get; set; }
    public string Action { get; set; } = string.Empty;
}

/// <summary>
/// Servicio que analiza las manos persistidas y calcula métricas de rendimiento.
/// </summary>
public class StrategyAnalyzerService
{
    /// <summary>
    /// Analiza todas las sesiones y genera métricas completas.
    /// </summary>
    public StrategyAnalysisResult AnalyzeSessions(List<GameSession> sessions)
    {
        var allHands = sessions.SelectMany(s => s.Hands).ToList();
        var bigBlind = sessions.FirstOrDefault()?.BigBlind ?? 0.50m;
        var result = Analyze(allHands, bigBlind);

        // Generar resumen de sesiones directamente desde GameSession
        result.Sessions = sessions
            .OrderByDescending(s => s.StartTime)
            .Select(s => new SessionSummary
            {
                SessionId = s.SessionId,
                TableName = s.TableName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Hands = s.Hands.Count,
                Profit = s.TotalProfit,
                BBPer100 = s.BBPer100
            })
            .ToList();

        return result;
    }

    /// <summary>
    /// Analiza un conjunto de manos y genera métricas completas.
    /// </summary>
    public StrategyAnalysisResult Analyze(List<HandRecord> rounds, decimal bigBlind = 0.50m)
    {
        var result = new StrategyAnalysisResult
        {
            TotalHands = rounds.Count
        };

        if (rounds.Count == 0)
            return result;

        // Clasificar resultados
        result.HandsWon = rounds.Count(r => r.Result == HandResult.Won);
        result.HandsLost = rounds.Count(r => r.Result == HandResult.Lost);
        result.HandsPush = rounds.Count(r => r.Result == HandResult.Push);
        result.HandsUnknown = rounds.Count(r => r.Result == HandResult.Unknown);

        // Profit
        var roundsWithResult = rounds.Where(r => r.Result != HandResult.Unknown).ToList();
        foreach (var round in roundsWithResult)
        {
            decimal diff = round.HeroStackEnd - round.HeroStackStart;
            result.TotalProfit += diff;
            if (diff > result.BiggestWin) result.BiggestWin = diff;
            if (diff < result.BiggestLoss) result.BiggestLoss = diff;
        }

        // BB/100
        if (bigBlind > 0 && rounds.Count > 0)
        {
            double totalBB = (double)(result.TotalProfit / bigBlind);
            result.BBPer100 = totalBB / rounds.Count * 100;
        }

        // Stats por posición
        result.StatsByPosition = AnalyzeByPosition(rounds);

        // Stats por street
        result.StatsByStreet = AnalyzeByStreet(rounds);

        // Stats por situación
        result.StatsBySituation = AnalyzeBySituation(rounds);

        // Equity vs Outcome
        result.EquityVsOutcomes = BuildEquityVsOutcome(rounds);
        result.EquityAccuracy = CalculateEquityAccuracy(result.EquityVsOutcomes);

        return result;
    }

    private Dictionary<TablePosition, PositionStats> AnalyzeByPosition(List<HandRecord> rounds)
    {
        var stats = new Dictionary<TablePosition, PositionStats>();

        foreach (var group in rounds.GroupBy(r => r.HeroPosition))
        {
            var list = group.ToList();
            stats[group.Key] = new PositionStats
            {
                Position = group.Key,
                Hands = list.Count,
                Won = list.Count(r => r.Result == HandResult.Won),
                Lost = list.Count(r => r.Result == HandResult.Lost),
                Profit = list.Where(r => r.Result != HandResult.Unknown)
                    .Sum(r => r.HeroStackEnd - r.HeroStackStart)
            };
        }

        return stats;
    }

    private Dictionary<BoardPosition, StreetStats> AnalyzeByStreet(List<HandRecord> rounds)
    {
        var stats = new Dictionary<BoardPosition, StreetStats>();
        var allDecisions = rounds.SelectMany(r => r.Decisions).ToList();

        foreach (var group in allDecisions.GroupBy(d => d.Street))
        {
            var list = group.ToList();
            stats[group.Key] = new StreetStats
            {
                Street = group.Key,
                TotalDecisions = list.Count,
                Bets = list.Count(d => d.ActionTaken.Contains("Bet")),
                Calls = list.Count(d => d.ActionTaken.Contains("Call")),
                Raises = list.Count(d => d.ActionTaken.Contains("Raise")),
                Folds = list.Count(d => d.ActionTaken.Contains("Fold")),
                Checks = list.Count(d => d.ActionTaken.Contains("Check")),
                AvgEquity = list.Average(d => d.EquityPercent),
                AvgPotOdds = list.Average(d => d.PotOddsPercent)
            };
        }

        return stats;
    }

    private Dictionary<HandSituation, SituationStats> AnalyzeBySituation(List<HandRecord> rounds)
    {
        var stats = new Dictionary<HandSituation, SituationStats>();

        foreach (var group in rounds.Where(r => r.Situation != HandSituation.None).GroupBy(r => r.Situation))
        {
            var list = group.ToList();
            stats[group.Key] = new SituationStats
            {
                Situation = group.Key,
                Hands = list.Count,
                Won = list.Count(r => r.Result == HandResult.Won),
                Profit = list.Where(r => r.Result != HandResult.Unknown)
                    .Sum(r => r.HeroStackEnd - r.HeroStackStart)
            };
        }

        return stats;
    }

    // AnalyzeSessions ahora es un método público que recibe List<GameSession> directamente

    /// <summary>
    /// Construye pares (equity predicha, resultado real) para cada decisión de la última calle jugada.
    /// </summary>
    private List<EquityVsOutcome> BuildEquityVsOutcome(List<HandRecord> rounds)
    {
        var items = new List<EquityVsOutcome>();

        foreach (var round in rounds.Where(r => r.Result != HandResult.Unknown && r.Decisions.Count > 0))
        {
            var lastDecision = round.Decisions.Last();
            items.Add(new EquityVsOutcome
            {
                Equity = lastDecision.EquityPercent,
                Won = round.Result == HandResult.Won,
                Profit = round.HeroStackEnd - round.HeroStackStart,
                Action = lastDecision.ActionTaken
            });
        }

        return items;
    }

    /// <summary>
    /// Calcula qué tan bien la equity predice el resultado real.
    /// Agrupa por buckets de 10% y compara winrate real vs equity predicha.
    /// Accuracy = 100 - promedio de diferencia absoluta por bucket.
    /// </summary>
    private double CalculateEquityAccuracy(List<EquityVsOutcome> items)
    {
        if (items.Count < 10) return 0;

        var buckets = items
            .GroupBy(i => (int)(i.Equity / 10) * 10)
            .Where(g => g.Count() >= 3)
            .ToList();

        if (buckets.Count == 0) return 0;

        double totalDiff = 0;
        int bucketCount = 0;

        foreach (var bucket in buckets)
        {
            double predictedEquity = bucket.Average(i => i.Equity);
            double actualWinRate = bucket.Count(i => i.Won) * 100.0 / bucket.Count();
            totalDiff += Math.Abs(predictedEquity - actualWinRate);
            bucketCount++;
        }

        return Math.Max(0, 100 - totalDiff / bucketCount);
    }

    /// <summary>
    /// Genera un resumen textual del análisis para mostrar en UI o logs.
    /// </summary>
    public string GenerateReport(StrategyAnalysisResult analysis)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine("       ANÁLISIS DE ESTRATEGIA");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"  Manos totales:    {analysis.TotalHands}");
        sb.AppendLine($"  Ganadas:          {analysis.HandsWon} ({analysis.WinRate:F1}%)");
        sb.AppendLine($"  Perdidas:         {analysis.HandsLost}");
        sb.AppendLine($"  Push:             {analysis.HandsPush}");
        sb.AppendLine($"  Sin resultado:    {analysis.HandsUnknown}");
        sb.AppendLine();
        sb.AppendLine($"  Profit total:     {analysis.TotalProfit:+0.00;-0.00}");
        sb.AppendLine($"  BB/100:           {analysis.BBPer100:+0.0;-0.0}");
        sb.AppendLine($"  Mayor ganancia:   {analysis.BiggestWin:+0.00}");
        sb.AppendLine($"  Mayor pérdida:    {analysis.BiggestLoss:+0.00;-0.00}");
        sb.AppendLine();

        if (analysis.EquityAccuracy > 0)
        {
            sb.AppendLine($"  Precisión equity: {analysis.EquityAccuracy:F1}%");
            sb.AppendLine();
        }

        // Por posición
        sb.AppendLine("─── POR POSICIÓN ───────────────────────");
        foreach (var pos in analysis.StatsByPosition.OrderByDescending(p => p.Value.Profit))
        {
            var s = pos.Value;
            sb.AppendLine($"  {s.Position,-12} {s.Hands,4} manos | WR: {s.WinRate,5:F1}% | Profit: {s.Profit,8:+0.00;-0.00}");
        }
        sb.AppendLine();

        // Por street
        sb.AppendLine("─── POR STREET ─────────────────────────");
        foreach (var street in analysis.StatsByStreet.OrderBy(s => s.Key))
        {
            var s = street.Value;
            sb.AppendLine($"  {s.Street,-8} {s.TotalDecisions,4} decisiones | " +
                $"Bet:{s.Bets} Call:{s.Calls} Raise:{s.Raises} Fold:{s.Folds} Check:{s.Checks} | " +
                $"Eq:{s.AvgEquity:F1}%");
        }
        sb.AppendLine();

        // Por situación
        if (analysis.StatsBySituation.Count > 0)
        {
            sb.AppendLine("─── POR SITUACIÓN ──────────────────────");
            foreach (var sit in analysis.StatsBySituation.OrderByDescending(s => s.Value.Hands))
            {
                var s = sit.Value;
                sb.AppendLine($"  {s.Situation,-22} {s.Hands,4} manos | WR: {s.WinRate,5:F1}% | Profit: {s.Profit,8:+0.00;-0.00}");
            }
            sb.AppendLine();
        }

        // Sesiones
        if (analysis.Sessions.Count > 0)
        {
            sb.AppendLine("─── SESIONES RECIENTES ─────────────────");
            foreach (var session in analysis.Sessions.Take(10))
            {
                var duration = session.EndTime - session.StartTime;
                var table = string.IsNullOrEmpty(session.TableName) ? "" : $"{session.TableName} | ";
                sb.AppendLine($"  {session.StartTime:dd/MM HH:mm} | {table}{session.Hands,4} manos | " +
                    $"{duration.TotalMinutes:F0}min | BB/100: {session.BBPer100:+0.0;-0.0} | " +
                    $"Profit: {session.Profit:+0.00;-0.00}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════");

        return sb.ToString();
    }
}
