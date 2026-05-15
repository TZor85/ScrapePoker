using OpenScrape.DecisionMaker.Services;
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
}
