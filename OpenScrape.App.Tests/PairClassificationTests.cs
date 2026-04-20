using Microsoft.Extensions.Options;

using OpenScrape.App.Aplication.UseCases;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using static OpenScrape.App.Tests.TestMakeInputHelper;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests para la clasificación de sub-tipos de par (PairClassification)
/// y para los comportamientos diferenciados de PostflopDecisionService según el tipo de par.
/// </summary>
[TestFixture]
public class PairClassificationTests
{
    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    private static HandEvaluation MakePairEval(Rank pairRank, Suit s1, Suit s2, Rank kicker)
    {
        var cards = new List<CardDataOuts>
        {
            C(pairRank, s1),
            C(pairRank, s2),
            C(kicker, Suit.Clubs)
        };
        return new HandEvaluation
        {
            Rank = HandRank.OnePair,
            Cards = cards,
            Kickers = [(int)kicker]
        };
    }

    // ─── Tests de ClassifyPair ──────────────────────────────────────────────────

    [Test]
    public void ClassifyPair_TopPair_HeroEmparejaCartaMasAlta()
    {
        // Board: A K 7 — hero tiene A en la mano
        var playerHand = new List<CardDataOuts> { C(Rank.Ace, Suit.Hearts), C(Rank.Two, Suit.Diamonds) };
        var community = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Clubs), C(Rank.King, Suit.Spades), C(Rank.Seven, Suit.Hearts)
        };
        var eval = MakePairEval(Rank.Ace, Suit.Hearts, Suit.Clubs, Rank.King);

        var result = UnifiedPokerCalculator.ClassifyPair(eval, playerHand, community);

        Assert.That(result, Is.EqualTo(PairClassification.TopPair));
    }

    [Test]
    public void ClassifyPair_BottomPair_HeroEmparejaCartaMasBaja()
    {
        // Board: A K 7 — hero tiene 7 en la mano
        var playerHand = new List<CardDataOuts> { C(Rank.Seven, Suit.Hearts), C(Rank.Two, Suit.Diamonds) };
        var community = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Clubs), C(Rank.King, Suit.Spades), C(Rank.Seven, Suit.Clubs)
        };
        var eval = MakePairEval(Rank.Seven, Suit.Hearts, Suit.Clubs, Rank.Ace);

        var result = UnifiedPokerCalculator.ClassifyPair(eval, playerHand, community);

        Assert.That(result, Is.EqualTo(PairClassification.BottomPair));
    }

    [Test]
    public void ClassifyPair_MiddlePair_HeroEmparejaCartaIntermedia()
    {
        // Board: A K 7 — hero tiene K en la mano (K es la carta del medio entre A y 7)
        var playerHand = new List<CardDataOuts> { C(Rank.King, Suit.Hearts), C(Rank.Two, Suit.Diamonds) };
        var community = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Clubs), C(Rank.King, Suit.Clubs), C(Rank.Seven, Suit.Hearts)
        };
        var eval = MakePairEval(Rank.King, Suit.Hearts, Suit.Clubs, Rank.Ace);

        var result = UnifiedPokerCalculator.ClassifyPair(eval, playerHand, community);

        Assert.That(result, Is.EqualTo(PairClassification.MiddlePair));
    }

    [Test]
    public void ClassifyPair_Overpair_PocketPairSuperiorATodasLasCartas()
    {
        // Board: J 8 3 — hero tiene pocket Kings (KK > J)
        var playerHand = new List<CardDataOuts> { C(Rank.King, Suit.Hearts), C(Rank.King, Suit.Diamonds) };
        var community = new List<CardDataOuts>
        {
            C(Rank.Jack, Suit.Clubs), C(Rank.Eight, Suit.Spades), C(Rank.Three, Suit.Hearts)
        };
        var eval = MakePairEval(Rank.King, Suit.Hearts, Suit.Diamonds, Rank.Jack);

        var result = UnifiedPokerCalculator.ClassifyPair(eval, playerHand, community);

        Assert.That(result, Is.EqualTo(PairClassification.Overpair));
    }

    [Test]
    public void ClassifyPair_PocketPairUnder_PocketPairInferiorAlBoard()
    {
        // Board: A K 8 — hero tiene pocket Nines (9 < A)
        var playerHand = new List<CardDataOuts> { C(Rank.Nine, Suit.Hearts), C(Rank.Nine, Suit.Diamonds) };
        var community = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Clubs), C(Rank.King, Suit.Spades), C(Rank.Eight, Suit.Hearts)
        };
        var eval = MakePairEval(Rank.Nine, Suit.Hearts, Suit.Diamonds, Rank.Ace);

        var result = UnifiedPokerCalculator.ClassifyPair(eval, playerHand, community);

        Assert.That(result, Is.EqualTo(PairClassification.PocketPairUnder));
    }

    [Test]
    public void ClassifyPair_BoardPaired_HeroNoContribuye()
    {
        // Board: K K 7 (board ya pareado) — hero tiene A J sin ningún K
        var playerHand = new List<CardDataOuts> { C(Rank.Ace, Suit.Hearts), C(Rank.Jack, Suit.Diamonds) };
        var community = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Clubs), C(Rank.King, Suit.Spades), C(Rank.Seven, Suit.Hearts)
        };
        // El evaluador detecta el par de Reyes del board
        var eval = MakePairEval(Rank.King, Suit.Clubs, Suit.Spades, Rank.Ace);

        var result = UnifiedPokerCalculator.ClassifyPair(eval, playerHand, community);

        Assert.That(result, Is.EqualTo(PairClassification.BoardPaired));
    }

    [Test]
    public void ClassifyPair_SinCommunityCards_RetornaNode()
    {
        var playerHand = new List<CardDataOuts> { C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Diamonds) };
        var community = new List<CardDataOuts>();
        var eval = new HandEvaluation { Rank = HandRank.OnePair, Cards = [], Kickers = [] };

        var result = UnifiedPokerCalculator.ClassifyPair(eval, playerHand, community);

        Assert.That(result, Is.EqualTo(PairClassification.None));
    }

    [Test]
    public void ClassifyPair_TopPair_BoardConCuatroCartas()
    {
        // Turn: A K 7 2 — hero tiene A
        var playerHand = new List<CardDataOuts> { C(Rank.Ace, Suit.Hearts), C(Rank.Four, Suit.Diamonds) };
        var community = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Clubs), C(Rank.King, Suit.Spades),
            C(Rank.Seven, Suit.Hearts), C(Rank.Two, Suit.Diamonds)
        };
        var eval = MakePairEval(Rank.Ace, Suit.Hearts, Suit.Clubs, Rank.King);

        var result = UnifiedPokerCalculator.ClassifyPair(eval, playerHand, community);

        Assert.That(result, Is.EqualTo(PairClassification.TopPair));
    }

    // ─── Tests de comportamiento diferenciado en PostflopDecisionService ───────

    private static PostflopDecisionService CreateService()
    {
        var profile = new StrategyProfile();
        var options = Options.Create(profile);
        var betSizing = new BetSizingService(options);
        var rangePolarizer = new RangePolarizer();
        return new PostflopDecisionService(options, betSizing, rangePolarizer);
    }

    [Test]
    public void DetermineAction_Overpair_VulnerabilidadMenorQueBottomPair()
    {
        // Overpair tiene adjValue = 55 + 0.8 = 55.8
        // BottomPair tiene adjValue = 55 + 3.0 = 58.0
        // Con equity=57 y OOP (isInPosition=false): Overpair apuesta en value, BottomPair no
        // (ThinValueIPOnly=true por defecto: thin value solo se apuesta IP)
        var service = CreateService();

        var resultOverpair = service.DetermineAction(MakeInput(
            equity: 57.0,
            street: BoardPosition.Turn,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: false,
            villainBetSize: BetSizeCategory.NoBet,
            heroHandRank: HandRank.OnePair,
            pairClassification: PairClassification.Overpair));

        var resultBottomPair = service.DetermineAction(MakeInput(
            equity: 57.0,
            street: BoardPosition.Turn,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: false,
            villainBetSize: BetSizeCategory.NoBet,
            heroHandRank: HandRank.OnePair,
            pairClassification: PairClassification.BottomPair));

        // Overpair con equity 57 supera adjValue (55.8) → apuesta en value
        Assert.That(resultOverpair.Action, Does.Contain("Value"),
            "Overpair con equity 57 debería apostar en value");
        // BottomPair con equity 57 no supera adjValue (58) → check OOP (thin value no aplica OOP)
        Assert.That(resultBottomPair.Action, Is.EqualTo("Check").Or.EqualTo("Fold"),
            "BottomPair con equity 57 OOP no debería apostar (adjValue=58 > 57)");
    }

    [Test]
    public void DetermineAction_Overpair_PuedeRaiseFacingBetConEquityAlta()
    {
        var service = CreateService();

        // Overpair facing bet con equity muy alta → debe poder raise
        var result = service.DetermineAction(MakeInput(
            equity: 80.0,
            street: BoardPosition.Turn,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.Medium,
            heroHandRank: HandRank.OnePair,
            pairClassification: PairClassification.Overpair));

        Assert.That(result.Action, Does.Contain("Raise"),
            "Overpair con equity muy alta facing bet debería poder raise");
    }

    [Test]
    public void DetermineAction_BottomPair_NoRaiseFacingBetConEquityAlta()
    {
        var service = CreateService();

        // BottomPair facing bet con equity alta → solo call, nunca raise
        var result = service.DetermineAction(MakeInput(
            equity: 80.0,
            street: BoardPosition.Turn,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.Medium,
            heroHandRank: HandRank.OnePair,
            pairClassification: PairClassification.BottomPair));

        Assert.That(result.Action, Is.Not.Contains("Raise"),
            "BottomPair no debería raise aunque equity sea alta");
        Assert.That(result.Action, Does.Contain("Call"),
            "BottomPair con equity alta facing bet debería call");
    }

    [Test]
    public void DetermineAction_TopPair_PuedeRaiseFacingBetConEquityAlta()
    {
        var service = CreateService();

        // TopPair facing bet con equity muy alta → puede raise (igual que Overpair)
        var result = service.DetermineAction(MakeInput(
            equity: 80.0,
            street: BoardPosition.Turn,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.Medium,
            heroHandRank: HandRank.OnePair,
            pairClassification: PairClassification.TopPair));

        Assert.That(result.Action, Does.Contain("Raise"),
            "TopPair con equity muy alta facing bet debería poder raise");
    }

    [Test]
    public void DetermineAction_BoardPaired_NoBluffCatchRiver()
    {
        var service = CreateService();

        // BoardPaired en river facing bet: hero no tiene par real → no bluff catch
        var result = service.DetermineAction(MakeInput(
            equity: 35.0,  // Por debajo del FoldBelow normal
            street: BoardPosition.River,
            situation: HandSituation.OpenRaise,
            boardTexture: "Paired",
            isInPosition: true,
            villainBetSize: BetSizeCategory.Small,
            heroHandRank: HandRank.OnePair,
            pairClassification: PairClassification.BoardPaired));

        Assert.That(result.Action, Is.EqualTo("Fold"),
            "BoardPaired en river no debe hacer bluff catch");
    }

    [Test]
    public void DetermineAction_OverpairVsBottomPair_BluffCatchDiferente()
    {
        var service = CreateService();
        // Equity marginal en river: Overpair puede bluff catch, BoardPaired no

        var resultOverpair = service.DetermineAction(MakeInput(
            equity: 36.0,
            street: BoardPosition.River,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.Small,
            heroHandRank: HandRank.OnePair,
            pairClassification: PairClassification.Overpair));

        var resultBoardPaired = service.DetermineAction(MakeInput(
            equity: 36.0,
            street: BoardPosition.River,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.Small,
            heroHandRank: HandRank.OnePair,
            pairClassification: PairClassification.BoardPaired));

        // Overpair puede call (bluff catch), BoardPaired debe fold
        Assert.That(resultBoardPaired.Action, Is.EqualTo("Fold"),
            "BoardPaired river no debe bluff catch");
        // Overpair y BoardPaired deben tener comportamientos diferentes
        Assert.That(resultOverpair.Action, Is.Not.EqualTo(resultBoardPaired.Action),
            "Overpair y BoardPaired deben comportarse diferente en river");
    }

    // ─── Tests de ReverseImpliedOdds diferenciado ───────────────────────────────

    [Test]
    public void ReverseImpliedOdds_Overpair_PenalidadMenorQueBottomPair()
    {
        var profile = new StrategyProfile();
        var boardChange = new BoardChangeResult(false, true, false, false, false, -1, 3);

        double penaltyOverpair = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            boardChange, HandRank.OnePair, hasFlushDraw: false,
            BoardPosition.Turn, isFacingBet: true, profile,
            PairClassification.Overpair);

        double penaltyBottomPair = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            boardChange, HandRank.OnePair, hasFlushDraw: false,
            BoardPosition.Turn, isFacingBet: true, profile,
            PairClassification.BottomPair);

        double penaltyBoardPaired = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            boardChange, HandRank.OnePair, hasFlushDraw: false,
            BoardPosition.Turn, isFacingBet: true, profile,
            PairClassification.BoardPaired);

        Assert.That(penaltyOverpair, Is.LessThan(penaltyBottomPair),
            "Overpair debe tener menor penalización que BottomPair");
        Assert.That(penaltyBottomPair, Is.LessThan(penaltyBoardPaired),
            "BottomPair debe tener menor penalización que BoardPaired");
    }

    [Test]
    public void ReverseImpliedOdds_Overpair_SinPenalidad_SinDraw()
    {
        var profile = new StrategyProfile();
        // Sin draw en board: penalidad base = 0 independientemente del tipo de par
        var boardChange = new BoardChangeResult(false, false, false, false, false, -1, 0);

        double penalty = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            boardChange, HandRank.OnePair, hasFlushDraw: false,
            BoardPosition.Turn, isFacingBet: true, profile,
            PairClassification.Overpair);

        Assert.That(penalty, Is.EqualTo(0.0),
            "Sin draw en board la penalidad debe ser 0 incluso para Overpair");
    }

    [Test]
    public void ReverseImpliedOdds_SoloAplicaEnTurn()
    {
        var profile = new StrategyProfile();
        var boardChange = new BoardChangeResult(false, true, false, false, false, -1, 3);

        double penaltyFlop = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            boardChange, HandRank.OnePair, false, BoardPosition.Flop, true, profile,
            PairClassification.BottomPair);

        double penaltyRiver = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            boardChange, HandRank.OnePair, false, BoardPosition.River, true, profile,
            PairClassification.BottomPair);

        Assert.That(penaltyFlop, Is.EqualTo(0.0), "Reverse implied no aplica en flop");
        Assert.That(penaltyRiver, Is.GreaterThan(0.0), "Reverse implied ahora aplica en river (reducido ×0.6)");
    }
}
