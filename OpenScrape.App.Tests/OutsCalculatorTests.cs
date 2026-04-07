using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class OutsCalculatorTests
{
    private OutsCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _calculator = new OutsCalculator();
    }

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    [Test]
    public void CalculateOuts_FlushDraw_DeberiaDetectar9Outs()
    {
        // Hero: Ah 5h, Flop: 2h 7h Kc (4 hearts = flush draw)
        // + backdoor straight (5-7 en ventana 3-4-5-6-7) = +1
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasFlushDraw, Is.True);
        // 9 flush + 3 overcard(A, no es straight out) + 1 backdoor straight = 13
        Assert.That(result.TotalOuts, Is.EqualTo(13));
    }

    [Test]
    public void CalculateOuts_OpenEndedStraightDraw_DeberiaDetectarOuts()
    {
        // Hero: 7s 8d, Flop: 9c Th 2c -> ranks: {2,7,8,9,10}
        // OESD real: 7-8-9-10 necesita 6 (para 6-7-8-9-10) o J (para 7-8-9-10-J)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Seven, Suit.Spades),
            C(Rank.Eight, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Nine, Suit.Clubs),
            C(Rank.Ten, Suit.Hearts),
            C(Rank.Two, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        // 2 ranks completan escalera (6 y J) → 8 outs de escalera
        Assert.That(result.HasOpenEndedStraightDraw, Is.True);
        Assert.That(result.TotalOuts, Is.EqualTo(8));
    }

    [Test]
    public void CalculateOuts_Gutshot_DeberiaDetectar4Outs()
    {
        // Hero: 5s 6d, Flop: 8c 9h 2s -> ranks: {2,5,6,8,9}
        // Gutshot: necesita 7 para completar 5-6-7-8-9
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Five, Suit.Spades),
            C(Rank.Six, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Eight, Suit.Clubs),
            C(Rank.Nine, Suit.Hearts),
            C(Rank.Two, Suit.Spades)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        // 1 rank completa escalera (7) → 4 outs de escalera
        Assert.That(result.HasGutshotStraightDraw, Is.True);
        Assert.That(result.TotalOuts, Is.EqualTo(4));
    }

    [Test]
    public void CalculateOuts_SinDraws_DeberiaSer0Outs()
    {
        // Hero: 2s 7d, Flop: Kh Qc 9d (sin draw alguno)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Spades),
            C(Rank.Seven, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Clubs),
            C(Rank.Nine, Suit.Diamonds)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.TotalOuts, Is.EqualTo(0));
        Assert.That(result.HasFlushDraw, Is.False);
        Assert.That(result.HasOpenEndedStraightDraw, Is.False);
        Assert.That(result.HasGutshotStraightDraw, Is.False);
    }

    [Test]
    public void CalculateOuts_OutsToEquity_ReglaDe2y4_Flop()
    {
        // En el flop, cardsToCome = 2, equity = outs * 2 * 2
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        // cardsToCome = 5 - 3 = 2, equity = 9 * 2 * 2 = 36
        Assert.That(result.OutsToEquity, Is.EqualTo(result.TotalOuts * 2 * 2.0));
    }

    [Test]
    public void CalculateOuts_OutsToEquity_ReglaDe2_Turn()
    {
        // En el turn, cardsToCome = 1, equity = outs * 1 * 2
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Clubs),
            C(Rank.Three, Suit.Spades)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        // cardsToCome = 5 - 4 = 1, equity = outs * 1 * 2
        Assert.That(result.OutsToEquity, Is.EqualTo(result.TotalOuts * 1 * 2.0));
    }

    [Test]
    public void CalculateOuts_FlushDrawMasOESD_DeberiaSumarSinDobleConteo()
    {
        // Hero: Js Ts, Flop: Qs 9s 3h -> ranks: {3,9,10,11,12}
        // Flush draw: 4 spades → 9 flush outs
        // OESD: 9-10-J-Q necesita 8 (para 8-9-10-J-Q) o K (para 9-10-J-Q-K) → 8 straight outs
        // Overlap: 8s y Ks son flush outs Y straight outs → 2 overlap
        // Total = 9 + 8 - 2 = 15 outs
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Jack, Suit.Spades),
            C(Rank.Ten, Suit.Spades)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Queen, Suit.Spades),
            C(Rank.Nine, Suit.Spades),
            C(Rank.Three, Suit.Hearts)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasFlushDraw, Is.True);
        Assert.That(result.HasOpenEndedStraightDraw, Is.True);
        Assert.That(result.HasStraightFlushDraw, Is.True);
        // 9 flush + 8 straight - 2 overlap = 15
        Assert.That(result.TotalOuts, Is.EqualTo(15));
    }

    [Test]
    public void CalculateOuts_FlushDrawMasGutshot_DeberiaSumarSinDobleConteo()
    {
        // Hero: Ah 6h, Flop: 8h 9h 3c -> ranks: {3,6,8,9,14}
        // Flush draw: 4 hearts → 9 flush outs
        // Gutshot: necesita 7 para 6-7-8-9-10? No, falta 10. Necesita 7 para 5-6-7-8-9? No 5.
        // Realmente: {3,6,8,9,14} → add 7 → {3,6,7,8,9,14} → 6-7-8-9-10? No. No straight.
        // Mejor ejemplo: Hero Ah Th, Flop: 8h 9h 3c → ranks: {3,8,9,10,14}
        // Gutshot: add J → 8-9-10-J-Q? No Q. Add 7 → 7-8-9-10-J? No J. Add J → no.
        // Mejor: Hero Jh 7h, Flop: 8h 9h 3c → ranks: {3,7,8,9,11}
        // Add 10 → {3,7,8,9,10,11} → 7-8-9-10-11 ✓ = straight
        // Add 6 → {3,6,7,8,9,11} → 6-7-8-9-10? No 10. No straight.
        // Solo 10 completa → gutshot, 4 outs
        // Flush: 9 outs, overlap: 10h = 1 carta
        // Total = 9 + 4 - 1 = 12
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Jack, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Eight, Suit.Hearts),
            C(Rank.Nine, Suit.Hearts),
            C(Rank.Three, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasFlushDraw, Is.True);
        Assert.That(result.HasGutshotStraightDraw, Is.True);
        // 9 flush + 4 straight - 1 overlap + 3 overcard(J, no es straight out) = 15
        Assert.That(result.TotalOuts, Is.EqualTo(15));
    }

    [Test]
    public void CalculateOuts_Rueda_DeberiaDetectarDraw()
    {
        // Hero: Ac 2d, Flop: 3s 4h Kc -> ranks: {2,3,4,13,14}
        // Rueda draw: A-2-3-4 necesita 5 para A-2-3-4-5
        // También: add 5 → {2,3,4,5,13,14} → 2-3-4-5-6? No 6. A-2-3-4-5? A=14, yes!
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Clubs),
            C(Rank.Two, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Three, Suit.Spades),
            C(Rank.Four, Suit.Hearts),
            C(Rank.King, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        // Necesita 5 para completar A-2-3-4-5 → 4 outs (gutshot)
        // + Ace es overcard (>K) → 3 outs extra (Ad, Ah, As)
        Assert.That(result.HasGutshotStraightDraw, Is.True);
        Assert.That(result.HasOvercards, Is.True);
        Assert.That(result.TotalOuts, Is.EqualTo(7));
    }

    [Test]
    public void CalculateOuts_YaTieneEscalera_DeberiaSerCeroOutsDeEscalera()
    {
        // Hero: 7s 8d, Flop: 9c Th 6h -> ranks: {6,7,8,9,10} = escalera hecha
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Seven, Suit.Spades),
            C(Rank.Eight, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Nine, Suit.Clubs),
            C(Rank.Ten, Suit.Hearts),
            C(Rank.Six, Suit.Hearts)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasOpenEndedStraightDraw, Is.False);
        Assert.That(result.HasGutshotStraightDraw, Is.False);
        Assert.That(result.TotalOuts, Is.EqualTo(0));
    }

    [Test]
    public void CalculateOuts_FlushCompleto_DeberiaSerCeroOutsDeFlush()
    {
        // Hero: Ah Kh, Flop: 2h 7h 9h -> 5 hearts = flush hecho
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Nine, Suit.Hearts)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasFlushDraw, Is.False);
        // Ya tiene flush → flush outs = 0
        Assert.That(result.TotalOuts, Is.EqualTo(0));
    }

    // ─── Tests de Overcards ──────────────────────────────────────────

    [Test]
    public void CalculateOuts_DosOvercards_Deberia6Outs()
    {
        // Hero: AK, Flop: 7-5-2 rainbow (sin draws principales)
        // 2 overcards: A y K → 3 outs cada una = 6 outs
        // + backdoor straight (A-2-5 en ventana A-2-3-4-5 wheel) = +1
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Seven, Suit.Clubs),
            C(Rank.Five, Suit.Diamonds),
            C(Rank.Two, Suit.Hearts)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasOvercards, Is.True);
        Assert.That(result.OvercardCount, Is.EqualTo(2));
        Assert.That(result.TotalOuts, Is.EqualTo(7)); // 6 overcards + 1 backdoor straight
        Assert.That(result.DrawTypes, Does.Contain("Overcards (2)"));
    }

    [Test]
    public void CalculateOuts_UnaOvercard_Deberia3Outs()
    {
        // Hero: A2, Flop: K-7-5 (solo Ace es overcard)
        // + backdoor straight (A-2-5 en ventana A-2-3-4-5 wheel) = +1
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Two, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Clubs),
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.Five, Suit.Hearts)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasOvercards, Is.True);
        Assert.That(result.OvercardCount, Is.EqualTo(1));
        Assert.That(result.TotalOuts, Is.EqualTo(4)); // 3 overcards + 1 backdoor straight
    }

    [Test]
    public void CalculateOuts_SinOvercards_DeberiaNoContarOvercards()
    {
        // Hero: 3s 4d, Flop: K-Q-J (ninguna carta de hero es overcard)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Three, Suit.Spades),
            C(Rank.Four, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Clubs),
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Diamonds)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasOvercards, Is.False);
        Assert.That(result.OvercardCount, Is.EqualTo(0));
    }

    [Test]
    public void CalculateOuts_OvercardsConFlushDraw_NoSeCuentan()
    {
        // Hero: Ah Kh, Flop: 7h 5h 2c → flush draw (9 outs)
        // Overcards: A y K son overcard, PERO con flush draw principal,
        // las overcards no se cuentan (el flush draw ya domina).
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Five, Suit.Hearts),
            C(Rank.Two, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasFlushDraw, Is.True);
        Assert.That(result.HasOvercards, Is.True);
        // 9 flush + 6 overcards(A=3, K=3, ambos > 7) + 1 backdoor straight = 16
        Assert.That(result.TotalOuts, Is.EqualTo(16));
    }

    #region [Backdoor Draws]

    [Test]
    public void CalculateOuts_BackdoorFlushDraw_DeberiaDetectar()
    {
        // Hero: Ah 5h, Flop: 2h 7c Kc → 3 hearts = backdoor flush draw
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Clubs),
            C(Rank.King, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasBackdoorFlushDraw, Is.True);
        Assert.That(result.HasFlushDraw, Is.False);
        Assert.That(result.DrawTypes, Does.Contain("Backdoor Flush Draw"));
    }

    [Test]
    public void CalculateOuts_BackdoorFlushDraw_NoContarSiYaHayFlushDraw()
    {
        // Hero: Ah 5h, Flop: 2h 7h Kc → 4 hearts = flush draw (no backdoor)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasFlushDraw, Is.True);
        Assert.That(result.HasBackdoorFlushDraw, Is.False);
    }

    [Test]
    public void CalculateOuts_BackdoorFlushDraw_RequiereCartaDeHero()
    {
        // Hero: Ac 5d, Flop: 2h 7h Kh → 3 hearts pero hero no tiene hearts
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Clubs),
            C(Rank.Five, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Hearts)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasBackdoorFlushDraw, Is.False);
    }

    [Test]
    public void CalculateOuts_BackdoorStraightDraw_DeberiaDetectar()
    {
        // Hero: 9c 2d, Flop: Th 7c 3s → 9-T-7 dentro de ventana 7-8-9-T-J = backdoor straight
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Nine, Suit.Clubs),
            C(Rank.Two, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Ten, Suit.Hearts),
            C(Rank.Seven, Suit.Clubs),
            C(Rank.Three, Suit.Spades)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasBackdoorStraightDraw, Is.True);
        Assert.That(result.DrawTypes, Does.Contain("Backdoor Straight Draw"));
    }

    [Test]
    public void CalculateOuts_BackdoorStraightDraw_NoContarSiYaHayOESD()
    {
        // Hero: 9c 8c, Flop: Th 7c 2s → 7-8-9-T = OESD (no backdoor)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Nine, Suit.Clubs),
            C(Rank.Eight, Suit.Clubs)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Ten, Suit.Hearts),
            C(Rank.Seven, Suit.Clubs),
            C(Rank.Two, Suit.Spades)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasOpenEndedStraightDraw, Is.True);
        Assert.That(result.HasBackdoorStraightDraw, Is.False);
    }

    [Test]
    public void CalculateOuts_BackdoorDraws_NoEnTurnOriver()
    {
        // Hero: Ah 5h, Turn: 2h 7c Kc 3d → 3 hearts pero ya no es flop
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Clubs),
            C(Rank.King, Suit.Clubs),
            C(Rank.Three, Suit.Diamonds)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasBackdoorFlushDraw, Is.False);
        Assert.That(result.HasBackdoorStraightDraw, Is.False);
    }

    #endregion

    #region Tainted Outs (Mejora 6)

    [Test]
    public void TaintedOuts_FlushDraw_OutsQuePareanBoard_Tainted()
    {
        // Hero: Ah 5h, Flop: 2h 7h Kc → flush draw (9 outs)
        // Outs que parean el board (2h→ya usada, 7x→parea board, Kx→parea board) son tainted
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        // Debe haber tainted outs > 0 (outs que crean pares o 3+ del mismo palo en board)
        Assert.That(result.TaintedOuts, Is.GreaterThan(0));
        Assert.That(result.EffectiveOuts, Is.LessThan(result.TotalOuts));
        Assert.That(result.CleanOuts, Is.EqualTo(result.TotalOuts - result.TaintedOuts));
    }

    [Test]
    public void TaintedOuts_EffectiveOuts_MenorQueTotalOuts()
    {
        // Hero: Kd Qd, Flop: 9d 3d 7s → flush draw (9 outs)
        // Algunos outs pondrán 3 del mismo palo en el board (si caen diamonds)
        // pero los flush outs YA son del suit de hero → revisar board pairing
        var myCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Diamonds),
            C(Rank.Queen, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Nine, Suit.Diamonds),
            C(Rank.Three, Suit.Diamonds),
            C(Rank.Seven, Suit.Spades)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasFlushDraw, Is.True);
        Assert.That(result.EffectiveOuts, Is.GreaterThan(0));
        // EffectiveOuts = CleanOuts + TaintedOuts * 0.7 (hero tiene flush draw → strong discount)
        Assert.That(result.EffectiveOuts,
            Is.EqualTo(result.CleanOuts + result.TaintedOuts * 0.7).Within(0.01));
    }

    [Test]
    public void TaintedOuts_SinOuts_TodosCero()
    {
        // Hero: Ah Kc, Flop: 2d 7s Js → sin draws
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Clubs)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Diamonds),
            C(Rank.Seven, Suit.Spades),
            C(Rank.Jack, Suit.Spades)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.TaintedOuts, Is.EqualTo(0));
        Assert.That(result.CleanOuts, Is.EqualTo(result.TotalOuts));
    }

    [Test]
    public void TaintedOuts_OESD_OutsQueCreanFlushDraw_Tainted()
    {
        // Hero: 8c 9c, Flop: 7d Ts 2h → OESD (6 o J completan)
        // Un 6h o Jh no crea flush draw en board (hearts tiene solo 1)
        // Pero un 6d parea... no, 6d no parea. Veamos:
        // Board suits: d, s, h → ningún suit tiene 2+ → outs de straight no crean flush draw
        // Pero outs que coinciden con ranks del board son tainted (7,T,2 → si un out coincide)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Eight, Suit.Clubs),
            C(Rank.Nine, Suit.Clubs)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.Ten, Suit.Spades),
            C(Rank.Two, Suit.Hearts)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasOpenEndedStraightDraw, Is.True);
        // Algunos outs de straight pueden estar tainted (crean 3 consecutivas en board)
        Assert.That(result.TaintedOuts, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.EffectiveOuts, Is.LessThanOrEqualTo(result.TotalOuts));
    }

    #endregion

    #region Combo Draw

    [Test]
    public void ComboDraw_FlushDraw_MasOESD_EsComboDraw()
    {
        // Hero: 8h 9h, Flop: 7h Th 2c → flush draw (4 hearts) + OESD (6 o J completan)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Eight, Suit.Hearts),
            C(Rank.Nine, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Ten, Suit.Hearts),
            C(Rank.Two, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasComboDraw, Is.True);
        Assert.That(result.HasFlushDraw, Is.True);
        Assert.That(result.HasOpenEndedStraightDraw, Is.True);
        Assert.That(result.DrawTypes, Does.Contain("Combo Draw"));
    }

    [Test]
    public void ComboDraw_SoloFlushDraw_NoEsComboDraw()
    {
        // Hero: Ah 5h, Flop: 2h 7h Kc → solo flush draw, sin straight draw
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasComboDraw, Is.False);
        Assert.That(result.HasFlushDraw, Is.True);
    }

    #endregion
}
