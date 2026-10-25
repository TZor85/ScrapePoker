using OpenScrape.App.Entities;
using OpenScrape.Contracts;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Services;

/// <summary>
/// Resultado de una decisión de calle (flop/turn/river).
/// </summary>
public record GameDecisionResult(
    string Action,
    string LogText,
    DecisionResult Decision,
    string? BoardTexture = null);

/// <summary>
/// Contrato para el coordinador del game loop.
/// Centraliza la lógica de decisión y análisis separada de la UI.
/// </summary>
public interface IGameCoordinator
{
    void ResetContext();

    // Decisiones por calle
    Task<GameDecisionResult> DetermineFlopAction(PlayerGameState state, CancellationToken cancellationToken = default);
    Task<GameDecisionResult> DetermineTurnAction(PlayerGameState state, CancellationToken cancellationToken = default);
    Task<GameDecisionResult> DetermineRiverAction(PlayerGameState state, CancellationToken cancellationToken = default);

    // Helpers
    BetSizeCategory GetOpponentBetSize(decimal maxBet, decimal potSize);
    OpponentType GetVillainType(PlayerGameState state, bool? heroIsInPosition = null);
    OpponentProfile? GetActiveVillainProfile(PlayerGameState state);
    void TrackVillainPostflopAction(PlayerGameState state, decimal maxBet, bool isPreflopAggressor, bool? heroIsInPosition = null);
    decimal GetVillainStack(PlayerGameState state);
    string GetActiveVillainId(PlayerGameState state);
    bool HeroBlocksTopBoardCard(PlayerGameState state);
    string FormatCardsForLog(PlayerGameState state, BoardPosition street);
    (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(PlayerGameState state, decimal maxBet, bool isHeroInPosition, HandSituation currentSituation);

    // Detection
    bool DetectNewHand(PlayerGameState state, bool handNumberChanged, string currentHand, ref string previousDealer, ref string previousSB, ref string previousBB);
}
