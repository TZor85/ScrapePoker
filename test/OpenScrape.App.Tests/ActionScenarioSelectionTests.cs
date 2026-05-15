using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Interfaces;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using OpenScrape.Features.ActionScenario;
using OpenScrape.Features.ActionScenario.Get;

namespace OpenScrape.App.Tests;

[TestFixture]
public class ActionScenarioSelectionTests
{
    [Test]
    public void SelectMatchingHands_FiltraPorBetSize()
    {
        var positions = new List<PlayerActionSequence>
        {
            new(
                Name: "OpenRaise",
                HeroPosition: TablePosition.Button.GetDescription(),
                OpenRaiser: null,
                ThreeBetPosition: null,
                Limper: null,
                Caller: null,
                Squeezer: null,
                BetSize: 2.5m,
                IsGreater: null,
                RaiserFolds: null,
                Hands: new List<Hand> { new("AK", true, "Raise x2.5", 100) }),
            new(
                Name: "OpenRaise",
                HeroPosition: TablePosition.Button.GetDescription(),
                OpenRaiser: null,
                ThreeBetPosition: null,
                Limper: null,
                Caller: null,
                Squeezer: null,
                BetSize: 3.0m,
                IsGreater: null,
                RaiserFolds: null,
                Hands: new List<Hand> { new("AK", true, "Raise x3", 100) })
        };

        var request = new ActionScenarioRequest
        {
            HeroPosition = TablePosition.Button,
            HandName = "AK",
            Suited = true,
            BetSize = 3.0m
        };

        var hands = GetActionScenario.SelectMatchingHands(positions, request);

        Assert.That(hands.Select(h => h.Action), Is.EqualTo(new[] { "Raise x3" }));
    }

    [TestCase(1, "Raise")]
    [TestCase(100, "Fold")]
    public void GetRandomAction_UsaRandomProviderInyectado(int randomNumber, string expectedAction)
    {
        var scenario = new GetActionScenario(null!, new FixedRandomProvider(randomNumber));
        var actions = new List<Hand>
        {
            new("AK", true, "Raise", 60),
            new("AK", true, "Fold", 40)
        };

        var method = typeof(GetActionScenario).GetMethod(
            "GetRandomAction",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        var action = method?.Invoke(scenario, new object[] { actions });

        Assert.That(action, Is.EqualTo(expectedAction));
    }

    private sealed class FixedRandomProvider : IRandomProvider
    {
        private readonly int _next;

        public FixedRandomProvider(int next)
        {
            _next = next;
        }

        public double NextDouble() => 0;

        public int Next(int minValue, int maxValue) => _next;
    }
}
