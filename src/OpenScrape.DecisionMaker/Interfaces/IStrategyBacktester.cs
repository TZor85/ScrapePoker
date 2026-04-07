using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de backtesting de estrategia.
/// </summary>
public interface IStrategyBacktester
{
    BacktestResult RunBacktest(List<HandRecord> hands, decimal bigBlind = 0.50m);
}
