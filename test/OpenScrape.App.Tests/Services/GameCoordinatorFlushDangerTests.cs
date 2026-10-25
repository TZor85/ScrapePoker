using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El flush danger que viaja al contexto del turn tiene que ser la transición
/// 2→3 palos que provocó la carta nueva, no "hay tres palos en el board".
/// Un flop monotone ya tenía tres y no es una carta que abrió el flush draw.
/// </summary>
[TestFixture]
public class GameCoordinatorFlushDangerTests
{
    [Test]
    public void FlushDrawAppeared_FlopMonotoneYTurnDeOtroPalo_EsFalso()
    {
        var board = FlopConPalos(3, Suit.Hearts);
        board.Add(Card(4, Suit.Spades));

        Assert.That(GameCoordinator.FlushDrawAppeared(board, previousCardCount: 3), Is.False);
    }

    [Test]
    public void FlushDrawAppeared_FlopDosPalosYTurnTercero_EsVerdadero()
    {
        var board = FlopConPalos(2, Suit.Hearts);
        board.Add(Card(4, Suit.Hearts));

        Assert.That(GameCoordinator.FlushDrawAppeared(board, previousCardCount: 3), Is.True);
    }

    [Test]
    public void FlushDrawAppeared_TurnQueCompletaElCuartoPalo_EsFalso()
    {
        var board = FlopConPalos(3, Suit.Hearts);
        board.Add(Card(4, Suit.Hearts));

        Assert.That(GameCoordinator.FlushDrawAppeared(board, previousCardCount: 3), Is.False);
    }

    private static List<BoardData> FlopConPalos(int cantidad, Suit palo)
    {
        var board = new List<BoardData>();
        for (var i = 1; i <= 3; i++)
            board.Add(Card(i, i <= cantidad ? palo : Suit.Clubs));

        return board;
    }

    private static BoardData Card(int rank, Suit suit) => new()
    {
        Force = rank,
        Suit = (int)suit,
        Position = BoardPosition.Flop,
        Location = rank
    };
}
