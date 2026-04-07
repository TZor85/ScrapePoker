using OpenScrape.Domain.Entities;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de auto-calibración de estrategia.
/// </summary>
public interface IAutoCalibrationService
{
    CalibrationResult Calibrate(
        IExploitabilityCalculator exploitabilityCalculator,
        StrategyProfile currentProfile);

    bool ShouldRecalibrate(IExploitabilityCalculator calculator);
    void RecordDecision();
    CalibrationPreview GetPreview(IExploitabilityCalculator calculator, StrategyProfile profile);
    void ResetCalibrationHistory();
    int GetDecisionsSinceLastCalibration();
}
