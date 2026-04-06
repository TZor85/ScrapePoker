using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;

namespace OpenScrape.DecisionMaker;

public class CalibrationResult
{
    public bool Success { get; set; }
    public int DecisionsAnalyzed { get; set; }
    public List<ParameterAdjustment> Adjustments { get; set; } = new();
    public double PreviousExploitability { get; set; }
    public double EstimatedNewExploitability { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
}

public class ParameterAdjustment
{
    public string ParameterName { get; set; } = string.Empty;
    public double OldValue { get; set; }
    public double NewValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeakCategory CausedBy { get; set; }
}

public class CalibrationPreview
{
    public List<ParameterAdjustment> ProposedAdjustments { get; set; } = new();
    public Dictionary<LeakCategory, double> LeakReduction { get; set; } = new();
    public double CurrentExploitability { get; set; }
    public double EstimatedNewExploitability { get; set; }
}

public class AutoCalibrationService
{
    private const int MinDecisionsForCalibration = 20;
    private const int RecalibrateThreshold = 50;
    private const double ExploitabilityCalibrationThreshold = 15.0;
    private const double MaxAdjustmentPerCycle = 5.0;
    private const double CalibrationMinImprovement = 2.0;

    private int _decisionsSinceLastCalibration = 0;
    private DateTime _lastCalibrationTime = DateTime.MinValue;

    public CalibrationResult Calibrate(
        ExploitabilityCalculator exploitabilityCalculator,
        StrategyProfile currentProfile)
    {
        var sessionAnalysis = exploitabilityCalculator.CalculateSessionAnalysis();
        
        if (sessionAnalysis.TotalDecisions < MinDecisionsForCalibration)
        {
            return new CalibrationResult
            {
                Success = false,
                DecisionsAnalyzed = sessionAnalysis.TotalDecisions,
                Message = $"No hay suficientes decisiones ({sessionAnalysis.TotalDecisions}/{MinDecisionsForCalibration})"
            };
        }

        var gtoDistance = exploitabilityCalculator.CalculateGTODistance();
        if (gtoDistance.DistanceMbb < ExploitabilityCalibrationThreshold)
        {
            return new CalibrationResult
            {
                Success = false,
                DecisionsAnalyzed = sessionAnalysis.TotalDecisions,
                PreviousExploitability = gtoDistance.DistanceMbb,
                Message = "La exploitabilidad ya está por debajo del threshold. La estrategia está cerca de GTO."
            };
        }

        var adjustments = new List<ParameterAdjustment>();
        var previousExploitability = sessionAnalysis.AverageExploitabilityMbb;

        foreach (var leak in sessionAnalysis.TopLeaks)
        {
            var adjustment = CalculateAdjustmentForLeak(leak, currentProfile);
            if (adjustment != null)
            {
                adjustments.Add(adjustment);
            }
        }

        if (adjustments.Count == 0)
        {
            return new CalibrationResult
            {
                Success = false,
                DecisionsAnalyzed = sessionAnalysis.TotalDecisions,
                PreviousExploitability = previousExploitability,
                Message = "No se requieren ajustes. Los leaks detectados son menores."
            };
        }

        var estimatedImprovement = CalculateEstimatedImprovement(adjustments);
        var estimatedNewExploitability = Math.Max(0, previousExploitability - estimatedImprovement);

        _decisionsSinceLastCalibration = 0;
        _lastCalibrationTime = DateTime.UtcNow;

        return new CalibrationResult
        {
            Success = true,
            DecisionsAnalyzed = sessionAnalysis.TotalDecisions,
            Adjustments = adjustments,
            PreviousExploitability = previousExploitability,
            EstimatedNewExploitability = estimatedNewExploitability,
            Message = $"Calibración aplicada. Estimado: {previousExploitability:F1} → {estimatedNewExploitability:F1} mbb/hand"
        };
    }

    public bool ShouldRecalibrate(ExploitabilityCalculator calculator)
    {
        var sessionAnalysis = calculator.CalculateSessionAnalysis();
        
        if (sessionAnalysis.TotalDecisions < MinDecisionsForCalibration)
            return false;

        if (_decisionsSinceLastCalibration < RecalibrateThreshold)
            return false;

        var gtoDistance = calculator.CalculateGTODistance();
        return gtoDistance.DistanceMbb > ExploitabilityCalibrationThreshold;
    }

    public void RecordDecision()
    {
        _decisionsSinceLastCalibration++;
    }

    public CalibrationPreview GetPreview(
        ExploitabilityCalculator calculator,
        StrategyProfile profile)
    {
        var sessionAnalysis = calculator.CalculateSessionAnalysis();
        var gtoDistance = calculator.CalculateGTODistance();

        var preview = new CalibrationPreview
        {
            CurrentExploitability = gtoDistance.DistanceMbb,
            EstimatedNewExploitability = gtoDistance.DistanceMbb
        };

        if (sessionAnalysis.TotalDecisions >= MinDecisionsForCalibration)
        {
            foreach (var leak in sessionAnalysis.TopLeaks)
            {
                var adjustment = CalculateAdjustmentForLeak(leak, profile);
                if (adjustment != null)
                {
                    preview.ProposedAdjustments.Add(adjustment);
                    preview.LeakReduction[leak.Category] = leak.AverageExploitabilityMbb;
                }
            }

            var improvement = CalculateEstimatedImprovement(preview.ProposedAdjustments);
            preview.EstimatedNewExploitability = Math.Max(0, preview.CurrentExploitability - improvement);
        }

        return preview;
    }

    private ParameterAdjustment? CalculateAdjustmentForLeak(LeakInfo leak, StrategyProfile currentProfile)
    {
        double adjustmentValue = Math.Min(leak.AverageExploitabilityMbb / 10, MaxAdjustmentPerCycle);
        
        if (adjustmentValue < 1.0)
            return null;

        return leak.Category switch
        {
            LeakCategory.OverBluffing => new ParameterAdjustment
            {
                ParameterName = "ThinValueAbove",
                OldValue = 45,
                NewValue = 45 + adjustmentValue,
                Reason = $"Over-bluffing detectado: {leak.Frequency} veces. Incrementar ThinValue para reducir bluffs marginales.",
                CausedBy = LeakCategory.OverBluffing
            },
            LeakCategory.OverCalling => new ParameterAdjustment
            {
                ParameterName = "FoldBelow",
                OldValue = 40,
                NewValue = 40 + adjustmentValue,
                Reason = $"Over-calling detectado: {leak.Frequency} veces. Incrementar FoldBelow para hacer más folds.",
                CausedBy = LeakCategory.OverCalling
            },
            LeakCategory.UnderBluffing => new ParameterAdjustment
            {
                ParameterName = "FoldBelow",
                OldValue = 40,
                NewValue = 40 - adjustmentValue,
                Reason = $"Under-bluffing detectado: {leak.Frequency} veces. Reducir FoldBelow para más betting.",
                CausedBy = LeakCategory.UnderBluffing
            },
            LeakCategory.UnderValue => new ParameterAdjustment
            {
                ParameterName = "ThinValueAbove",
                OldValue = 45,
                NewValue = 45 - adjustmentValue,
                Reason = $"Under-value detectado: {leak.Frequency} veces. Reducir ThinValue para más value betting.",
                CausedBy = LeakCategory.UnderValue
            },
            _ => null
        };
    }

    private double CalculateEstimatedImprovement(List<ParameterAdjustment> adjustments)
    {
        double totalImprovement = 0;
        
        foreach (var adjustment in adjustments)
        {
            double improvement = adjustment.NewValue - adjustment.OldValue;
            
            if (adjustment.ParameterName == "FoldBelow")
            {
                totalImprovement += Math.Abs(improvement) * 2;
            }
            else if (adjustment.ParameterName == "ThinValueAbove")
            {
                totalImprovement += Math.Abs(improvement) * 1.5;
            }
        }

        return Math.Min(totalImprovement, 20);
    }

    public void ResetCalibrationHistory()
    {
        _decisionsSinceLastCalibration = 0;
        _lastCalibrationTime = DateTime.MinValue;
    }

    public int GetDecisionsSinceLastCalibration() => _decisionsSinceLastCalibration;
}