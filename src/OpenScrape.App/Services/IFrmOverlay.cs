using OpenScrape.App.Aplication.UseCases;
using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Services;

/// <summary>
/// Contrato del overlay consumido por <see cref="IUiSyncService"/>. Expone
/// solo los métodos de actualización invocados por el coordinator; no
/// filtra detalles de WinForms al resto del sistema.
/// </summary>
public interface IFrmOverlay
{
    void UpdateAction(string action);
    void UpdatePotOddsPercentage(string potOdds);
    void UpdateEquityPercentage(string equity);
    void UpdateStreetPhase(string phase);
    void UpdateSituacion(string situacion);
    void UpdateHandStrength(HandRank handRank, KickerStrength kicker, bool hasComboDraw);
    void UpdateBoardTexture(string? texture, double wetnessScore);
    void UpdateStreetIndicator(string street);
    void UpdateFoldEquity(double foldEquity);
    void UpdateEVWithFoldEquity(double ev);
    void UpdateSuggestedBetSize(double? betSize);
    void UpdateTableName(string tableName);
    void UpdateWithDecision(DecisionResult decision, string street);
    void ClearAll();
}
