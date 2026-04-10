using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Algorithms;

/// <summary>
/// Interfaz para análisis de textura del board.
/// </summary>
public interface IBoardTextureAnalyzer
{
    BoardTextureResult Analyze(List<int> ranks, List<int> suits);
    BoardTextureResult Analyze(List<CardDataOuts> communityCards);
    BoardChangeResult AnalyzeBoardChange(List<int> previousRanks, List<int> previousSuits, int newCardRank, int newCardSuit);
    BoardChangeResult AnalyzeBoardChange(List<CardDataOuts> previousBoard, CardDataOuts newCard);
    BoardChangeResult AnalyzeInitialBoard(List<int> ranks, List<int> suits);
    BoardChangeResult AnalyzeInitialBoard(List<CardDataOuts> communityCards);
    /// <summary>S22.2: Clasifica la river card como Blank, Neutral o Scare.</summary>
    RiverCardType ClassifyRiverCard(BoardChangeResult boardChange);
}
