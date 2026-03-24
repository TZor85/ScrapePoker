using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class BoardTextureAnalyzerTests
{
    private BoardTextureAnalyzer _analyzer;

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    [SetUp]
    public void Setup()
    {
        _analyzer = new BoardTextureAnalyzer();
    }

    [Test]
    public void Analyze_FlopMonotone_DeberiaSerWet()
    {
        // Flop: Ah Kh Qh (monotone)
        var cards = new List<CardDataOuts> { C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Hearts), C(Rank.Queen, Suit.Hearts) };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.IsMonotone, Is.True);
        Assert.That(result.HasFlushPossibility, Is.True);
        Assert.That(result.Category, Is.EqualTo(BoardTextureCategory.Wet));
        Assert.That(result.SimplifiedTexture, Is.EqualTo("Monotone"));
    }

    [Test]
    public void Analyze_FlopRainbowDisconnected_DeberiaSerDry()
    {
        // Flop: 2c 7d Ks (rainbow, desconectado)
        var cards = new List<CardDataOuts> { C(Rank.Two, Suit.Clubs), C(Rank.Seven, Suit.Diamonds), C(Rank.King, Suit.Spades) };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.IsRainbow, Is.True);
        Assert.That(result.HasFlushPossibility, Is.False);
        Assert.That(result.Category, Is.EqualTo(BoardTextureCategory.Dry));
        Assert.That(result.SimplifiedTexture, Is.EqualTo("Dry"));
    }

    [Test]
    public void Analyze_FlopPaired_DeberiaSerPaired()
    {
        // Flop: 8c 8d Ks
        var cards = new List<CardDataOuts> { C(Rank.Eight, Suit.Clubs), C(Rank.Eight, Suit.Diamonds), C(Rank.King, Suit.Spades) };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.IsPaired, Is.True);
        Assert.That(result.Category, Is.EqualTo(BoardTextureCategory.Paired));
        Assert.That(result.SimplifiedTexture, Is.EqualTo("Paired"));
    }

    [Test]
    public void Analyze_FlopTwoToneConnected_DeberiaSerSemiWetOWet()
    {
        // Flop: 9h Th Jc (two-tone, connected)
        var cards = new List<CardDataOuts> { C(Rank.Nine, Suit.Hearts), C(Rank.Ten, Suit.Hearts), C(Rank.Jack, Suit.Clubs) };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.IsTwoTone, Is.True);
        Assert.That(result.IsConnected, Is.True);
        Assert.That(result.HasStraightPossibility, Is.True);
        Assert.That(result.WetnessScore, Is.GreaterThan(30));
    }

    [Test]
    public void Analyze_BroadwayHeavy_DeberiaDetectarBroadway()
    {
        // Flop: Kh Qd Js
        var cards = new List<CardDataOuts> { C(Rank.King, Suit.Hearts), C(Rank.Queen, Suit.Diamonds), C(Rank.Jack, Suit.Spades) };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.IsBroadwayHeavy, Is.True);
    }

    [Test]
    public void Analyze_LowBoard_DeberiaDetectarLowBoard()
    {
        // Flop: 2c 4d 6s
        var cards = new List<CardDataOuts> { C(Rank.Two, Suit.Clubs), C(Rank.Four, Suit.Diamonds), C(Rank.Six, Suit.Spades) };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.IsLowBoard, Is.True);
    }

    [Test]
    public void Analyze_TurnConFlush_DeberiaAumentarWetness()
    {
        // Turn: Ah 5h 9h 3c (3 hearts = flush possibility)
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts), C(Rank.Five, Suit.Hearts),
            C(Rank.Nine, Suit.Hearts), C(Rank.Three, Suit.Clubs)
        };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.HasFlushPossibility, Is.True);
        Assert.That(result.WetnessScore, Is.GreaterThan(20));
    }

    [Test]
    public void Analyze_River5Cartas_DeberiaAnalizarCorrectamente()
    {
        // River: 2c 7d Ks 4h Jc (5 cartas, dry)
        var cards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Clubs), C(Rank.Seven, Suit.Diamonds),
            C(Rank.King, Suit.Spades), C(Rank.Four, Suit.Hearts),
            C(Rank.Jack, Suit.Clubs)
        };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.Category, Is.Not.EqualTo(BoardTextureCategory.Paired));
    }

    [Test]
    public void Analyze_MenosDe3Cartas_DeberiaRetornarDry()
    {
        var cards = new List<CardDataOuts> { C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Hearts) };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.Category, Is.EqualTo(BoardTextureCategory.Dry));
        Assert.That(result.WetnessScore, Is.EqualTo(0));
    }

    [Test]
    public void Analyze_VersionConRanksYSuits_DeberiaFuncionarIgual()
    {
        // Ranks como ints, suits como ints
        var ranks = new List<int> { 14, 13, 12 }; // A, K, Q
        var suits = new List<int> { 1, 1, 1 }; // all hearts

        var result = _analyzer.Analyze(ranks, suits);

        Assert.That(result.IsMonotone, Is.True);
        Assert.That(result.IsBroadwayHeavy, Is.True);
    }

    [Test]
    public void WetnessScore_MonotoneConnectedBroadway_DeberiaSerAlto()
    {
        // Board más wet posible: Jh Qh Kh (monotone + connected + broadway)
        var cards = new List<CardDataOuts> { C(Rank.Jack, Suit.Hearts), C(Rank.Queen, Suit.Hearts), C(Rank.King, Suit.Hearts) };
        var result = _analyzer.Analyze(cards);

        Assert.That(result.WetnessScore, Is.GreaterThanOrEqualTo(60));
    }

    [Test]
    public void SimplifiedTexture_MapeoRetrocompatible()
    {
        // Dry → "Dry"
        var dry = new BoardTextureResult(BoardTextureCategory.Dry, 10, false, false, true, false, false, false, true, false, false);
        Assert.That(dry.SimplifiedTexture, Is.EqualTo("Dry"));

        // SemiDry → "Dry"
        var semiDry = new BoardTextureResult(BoardTextureCategory.SemiDry, 20, false, true, false, false, false, false, false, false, false);
        Assert.That(semiDry.SimplifiedTexture, Is.EqualTo("Dry"));

        // SemiWet → "Coordinated"
        var semiWet = new BoardTextureResult(BoardTextureCategory.SemiWet, 40, false, true, false, false, true, false, false, true, true);
        Assert.That(semiWet.SimplifiedTexture, Is.EqualTo("Coordinated"));

        // Wet + Monotone → "Monotone"
        var wet = new BoardTextureResult(BoardTextureCategory.Wet, 70, true, false, false, false, true, true, false, true, true);
        Assert.That(wet.SimplifiedTexture, Is.EqualTo("Monotone"));

        // Paired → "Paired"
        var paired = new BoardTextureResult(BoardTextureCategory.Paired, 15, false, false, true, true, false, false, false, false, false);
        Assert.That(paired.SimplifiedTexture, Is.EqualTo("Paired"));
    }

    // --- Tests de BoardChangeResult / AnalyzeBoardChange ---

    [Test]
    public void AnalyzeBoardChange_FlushDraw_Turn2hEnQh3h7s()
    {
        // Flop: Qh(12,1) 3h(3,1) 7s(7,4) → Turn: 2h(2,1) → 3 hearts = flush draw (no completado)
        // Flush completado requiere 4+ del mismo palo en board
        var prevRanks = new List<int> { 12, 3, 7 };
        var prevSuits = new List<int> { 1, 1, 4 }; // hearts=1, spades=4
        var result = _analyzer.AnalyzeBoardChange(prevRanks, prevSuits, 2, 1); // 2h

        Assert.That(result.FlushCompleted, Is.False);
        Assert.That(result.FlushDrawAppeared, Is.True);
        Assert.That(result.DangerLevel, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void AnalyzeBoardChange_FlushCompleted_4DelMismoPalo()
    {
        // Board: Qh 3h 7h → Turn: 2h → 4 hearts = flush completado
        var prevRanks = new List<int> { 12, 3, 7 };
        var prevSuits = new List<int> { 1, 1, 1 }; // 3 hearts
        var result = _analyzer.AnalyzeBoardChange(prevRanks, prevSuits, 2, 1); // 2h → 4 hearts

        Assert.That(result.FlushCompleted, Is.True);
        Assert.That(result.CompletedFlushSuit, Is.EqualTo(1));
        Assert.That(result.DangerLevel, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void AnalyzeBoardChange_SafeCard_NoDanger()
    {
        // Flop: Qh 3h 7s → Turn: 2c (no completa nada)
        var prevRanks = new List<int> { 12, 3, 7 };
        var prevSuits = new List<int> { 1, 1, 4 };
        var result = _analyzer.AnalyzeBoardChange(prevRanks, prevSuits, 2, 2); // 2c (clubs=2)

        Assert.That(result.FlushCompleted, Is.False);
        Assert.That(result.StraightCompleted, Is.False);
        Assert.That(result.BoardPaired, Is.False);
        Assert.That(result.DangerLevel, Is.EqualTo(0));
    }

    [Test]
    public void AnalyzeBoardChange_BoardPaired()
    {
        // Flop: Qh 3d 7s → Turn: 7c → board paired
        var prevRanks = new List<int> { 12, 3, 7 };
        var prevSuits = new List<int> { 1, 3, 4 };
        var result = _analyzer.AnalyzeBoardChange(prevRanks, prevSuits, 7, 2); // 7c

        Assert.That(result.BoardPaired, Is.True);
        Assert.That(result.DangerLevel, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void AnalyzeBoardChange_OvercardAppeared()
    {
        // Flop: 3h 5d 7s → Turn: Ac → overcard
        var prevRanks = new List<int> { 3, 5, 7 };
        var prevSuits = new List<int> { 1, 3, 4 };
        var result = _analyzer.AnalyzeBoardChange(prevRanks, prevSuits, 14, 2); // Ac

        Assert.That(result.OvercardAppeared, Is.True);
        Assert.That(result.DangerLevel, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void AnalyzeBoardChange_StraightCompleted()
    {
        // Flop: 9c Td Jh → Turn: 8s → straight completado (8-9-T-J)
        var prevRanks = new List<int> { 9, 10, 11 };
        var prevSuits = new List<int> { 2, 3, 1 };
        var result = _analyzer.AnalyzeBoardChange(prevRanks, prevSuits, 8, 4); // 8s

        Assert.That(result.StraightCompleted, Is.True);
        Assert.That(result.DangerLevel, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void AnalyzeBoardChange_River_FlushStillDangerous()
    {
        // Board turn: Qh 3h 7s 2h → River: Tc (no completa flush pero 3 hearts persisten)
        var prevRanks = new List<int> { 12, 3, 7, 2 };
        var prevSuits = new List<int> { 1, 1, 4, 1 }; // 3 hearts ya
        var result = _analyzer.AnalyzeBoardChange(prevRanks, prevSuits, 10, 2); // Tc

        // Flush ya estaba completada antes, la nueva carta no cambia eso
        // Pero el board ya tenía 3 hearts, y ahora con 4 cartas previas ya tenemos flush en board
        Assert.That(result.OvercardAppeared, Is.False); // Tc(10) < Qh(12)
    }

    [Test]
    public void AnalyzeBoardChange_MenosDe3CartasPrevias_Safe()
    {
        var result = _analyzer.AnalyzeBoardChange(new List<int> { 12, 3 }, new List<int> { 1, 1 }, 7, 4);
        Assert.That(result.DangerLevel, Is.EqualTo(0));
    }

    // --- Tests de AnalyzeInitialBoard (Sprint 4) ---

    [Test]
    public void AnalyzeInitialBoard_TwoTone_FlushDrawPresente()
    {
        // Ah Qh 3d — dos hearts
        var result = _analyzer.AnalyzeInitialBoard(new List<int> { 14, 12, 3 }, new List<int> { 1, 1, 2 });

        Assert.That(result.FlushDrawAppeared, Is.True, "2-tone flop → flush draw presente");
        Assert.That(result.FlushCompleted, Is.False, "Flop nunca completa flush");
        Assert.That(result.DangerLevel, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void AnalyzeInitialBoard_Monotone_DangerAlto()
    {
        // Ah 5h 9h — tres hearts (monotone)
        var result = _analyzer.AnalyzeInitialBoard(new List<int> { 14, 5, 9 }, new List<int> { 1, 1, 1 });

        Assert.That(result.FlushDrawAppeared, Is.True);
        Assert.That(result.DangerLevel, Is.GreaterThanOrEqualTo(3), "Monotone flop → danger alto");
    }

    [Test]
    public void AnalyzeInitialBoard_Rainbow_SinFlushDraw()
    {
        // 2c 7d Ks — rainbow desconectado
        var result = _analyzer.AnalyzeInitialBoard(new List<int> { 2, 7, 13 }, new List<int> { 1, 2, 3 });

        Assert.That(result.FlushDrawAppeared, Is.False, "Rainbow → sin flush draw");
        Assert.That(result.BoardPaired, Is.False);
    }

    [Test]
    public void AnalyzeInitialBoard_Paired_BoardPaired()
    {
        // 8c 8d Ks — paired
        var result = _analyzer.AnalyzeInitialBoard(new List<int> { 8, 8, 13 }, new List<int> { 1, 2, 3 });

        Assert.That(result.BoardPaired, Is.True, "Board paired desde flop");
        Assert.That(result.DangerLevel, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void AnalyzeInitialBoard_Connected_StraightDraw()
    {
        // Jh Td 9c — connected rainbow
        var result = _analyzer.AnalyzeInitialBoard(new List<int> { 11, 10, 9 }, new List<int> { 1, 2, 3 });

        Assert.That(result.DangerLevel, Is.GreaterThanOrEqualTo(1), "Connected board → straight draw danger");
        Assert.That(result.FlushCompleted, Is.False);
        Assert.That(result.StraightCompleted, Is.False);
    }

    // --- Tests Sprint 5: Wet vs Coordinated ---

    [Test]
    public void SimplifiedTexture_Wet_NoMonotone_RetornaWet()
    {
        // Wet (60+ wetness), NOT monotone → "Wet"
        var wet = new BoardTextureResult(BoardTextureCategory.Wet, 70, false, true, false, false, true, true, false, true, true);
        Assert.That(wet.SimplifiedTexture, Is.EqualTo("Wet"));
    }

    [Test]
    public void SimplifiedTexture_SemiWet_RetornaCoordinated()
    {
        // SemiWet (35-60 wetness) → "Coordinated"
        var semiWet = new BoardTextureResult(BoardTextureCategory.SemiWet, 45, false, true, false, false, true, false, false, true, true);
        Assert.That(semiWet.SimplifiedTexture, Is.EqualTo("Coordinated"));
    }
}
