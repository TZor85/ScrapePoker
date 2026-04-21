using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

/// <summary>
/// Resultado de una decisión de calle (flop/turn/river).
/// </summary>
public record GameDecisionResult(
    string Action,
    string LogText,
    PostflopDecisionResult? Decision = null,
    BoardChangeResult? BoardChange = null,
    string? BoardTexture = null);

/// <summary>
/// Contrato para el coordinador del game loop.
/// Centraliza la lógica de decisión y análisis separada de la UI.
/// </summary>
public interface IGameCoordinator
{
    PokerCalculationResult? FlopResult { get; }
    PokerCalculationResult? TurnResult { get; }
    PokerCalculationResult? RiverResult { get; }
    TurnBoardTexture TurnBoardTexture { get; set; }
    RiverBoardTexture RiverBoardTexture { get; set; }

    void ResetContext();

    // Decisiones por calle
    GameDecisionResult DetermineFlopAction(PlayerGameState state);
    GameDecisionResult DetermineTurnAction(PlayerGameState state);
    GameDecisionResult DetermineRiverAction(PlayerGameState state);

    // Almacenar resultados de cálculo (llenados por FrmMain tras OCR)
    void SetFlopResult(PokerCalculationResult result);
    void SetTurnResult(PokerCalculationResult result);
    void SetRiverResult(PokerCalculationResult result);

    // Helpers
    BetSizeCategory GetOpponentBetSize(decimal maxBet, decimal potSize);
    OpponentType GetVillainType(PlayerGameState state, bool? heroIsInPosition = null);
    OpponentProfile? GetActiveVillainProfile(PlayerGameState state);
    void TrackVillainPostflopAction(PlayerGameState state, decimal maxBet, bool isPreflopAggressor, bool? heroIsInPosition = null);
    decimal GetVillainStack(PlayerGameState state);
    string GetActiveVillainId(PlayerGameState state);
    bool HeroBlocksTopBoardCard(PlayerGameState state);
    string AdjustBetSize(string action, decimal heroStack, decimal potSize, int numOpponents, bool isPaired, bool isCoordinated, bool isDry, bool isInPosition);
    string FormatCardsForLog(PlayerGameState state, BoardPosition street);
    (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(PlayerGameState state, decimal maxBet, bool isHeroInPosition, HandSituation currentSituation);

    // Board analysis
    BoardChangeResult AnalyzeBoardChange(List<BoardData> boardCards, int previousCardCount);
    TurnBoardTexture AnalyzeTurnBoardTexture(List<BoardData> boardCards);
    RiverBoardTexture AnalyzeRiverBoardTexture(List<BoardData> boardCards);

    // Detection
    bool DetectNewHand(PlayerGameState state, bool handNumberChanged, string currentHand, ref string previousDealer, ref string previousSB, ref string previousBB);
}
