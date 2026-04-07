using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de análisis de estrategia.
/// </summary>
public interface IStrategyAnalyzerService
{
    StrategyAnalysisResult AnalyzeSessions(List<GameSession> sessions, List<HandRecord> allHands);
    StrategyAnalysisResult Analyze(List<HandRecord> rounds, decimal bigBlind = 0.50m);
    string GenerateReport(StrategyAnalysisResult analysis);
}
