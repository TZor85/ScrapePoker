using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Services;

namespace OpenScrape.App.Tests;

[TestFixture]
public class GameLoopStateMachineTests
{
    private GameLoopStateMachine _stateMachine;

    [SetUp]
    public void Setup()
    {
        var logger = NullLogger<GameLoopStateMachine>.Instance;
        _stateMachine = new GameLoopStateMachine(logger);
    }

    [Test]
    public void EstadoInicial_DeberiaSer_WaitingForHand()
    {
        Assert.That(_stateMachine.CurrentState, Is.EqualTo(GameState.WaitingForHand));
    }

    [Test]
    public void TryTransition_WaitingToHandDetected_DeberiaSerValida()
    {
        var result = _stateMachine.TryTransition(GameState.HandDetected);

        Assert.That(result, Is.True);
        Assert.That(_stateMachine.CurrentState, Is.EqualTo(GameState.HandDetected));
    }

    [Test]
    public void TryTransition_WaitingToFlop_DeberiaSerInvalida()
    {
        var result = _stateMachine.TryTransition(GameState.FlopDetected);

        Assert.That(result, Is.False);
        Assert.That(_stateMachine.CurrentState, Is.EqualTo(GameState.WaitingForHand));
    }

    [Test]
    public void TryTransition_FlujoCompleto_Preflop_Flop_Turn_River()
    {
        Assert.That(_stateMachine.TryTransition(GameState.HandDetected), Is.True);
        Assert.That(_stateMachine.TryTransition(GameState.PreflopAction), Is.True);
        Assert.That(_stateMachine.TryTransition(GameState.FlopDetected), Is.True);
        Assert.That(_stateMachine.TryTransition(GameState.FlopAction), Is.True);
        Assert.That(_stateMachine.TryTransition(GameState.TurnDetected), Is.True);
        Assert.That(_stateMachine.TryTransition(GameState.TurnAction), Is.True);
        Assert.That(_stateMachine.TryTransition(GameState.RiverDetected), Is.True);
        Assert.That(_stateMachine.TryTransition(GameState.RiverAction), Is.True);
        Assert.That(_stateMachine.TryTransition(GameState.HandComplete), Is.True);
        Assert.That(_stateMachine.TryTransition(GameState.WaitingForHand), Is.True);
    }

    [Test]
    public void TryTransition_HandComplete_SoloPuedeIrAWaiting()
    {
        _stateMachine.TryTransition(GameState.HandDetected);
        _stateMachine.TryTransition(GameState.PreflopAction);
        _stateMachine.TryTransition(GameState.HandComplete);

        Assert.That(_stateMachine.TryTransition(GameState.FlopDetected), Is.False);
        Assert.That(_stateMachine.TryTransition(GameState.WaitingForHand), Is.True);
    }

    [Test]
    public void Reset_DeberiaVolverAWaitingForHand()
    {
        _stateMachine.TryTransition(GameState.HandDetected);
        _stateMachine.TryTransition(GameState.PreflopAction);
        _stateMachine.TryTransition(GameState.FlopDetected);

        _stateMachine.Reset();

        Assert.That(_stateMachine.CurrentState, Is.EqualTo(GameState.WaitingForHand));
    }

    [Test]
    public void IsPreflop_DeberiaSerTrue_EnHandDetectedYPreflopAction()
    {
        _stateMachine.TryTransition(GameState.HandDetected);
        Assert.That(_stateMachine.IsPreflop, Is.True);

        _stateMachine.TryTransition(GameState.PreflopAction);
        Assert.That(_stateMachine.IsPreflop, Is.True);
    }

    [Test]
    public void IsFlop_DeberiaSerTrue_EnFlopDetectedYFlopAction()
    {
        _stateMachine.TryTransition(GameState.HandDetected);
        _stateMachine.TryTransition(GameState.PreflopAction);
        _stateMachine.TryTransition(GameState.FlopDetected);
        Assert.That(_stateMachine.IsFlop, Is.True);

        _stateMachine.TryTransition(GameState.FlopAction);
        Assert.That(_stateMachine.IsFlop, Is.True);
    }

    [Test]
    public void TryTransition_PreflopAction_PuedeIrAWaiting_FoldEarly()
    {
        _stateMachine.TryTransition(GameState.HandDetected);
        _stateMachine.TryTransition(GameState.PreflopAction);

        var result = _stateMachine.TryTransition(GameState.WaitingForHand);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsWaiting_DeberiaSerTrue_EnEstadoInicial()
    {
        Assert.That(_stateMachine.IsWaiting, Is.True);
    }
}
