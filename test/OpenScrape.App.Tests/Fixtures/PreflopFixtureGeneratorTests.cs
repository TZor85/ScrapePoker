using OpenScrape.App.Aplication;
using OpenScrape.App.Entities;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Genera los fixtures preflop desde el comportamiento actual del motor.
/// No corre en CI: los fixtures resultantes se versionan y los usa el replay.
/// </summary>
[TestFixture]
[Explicit]
public class PreflopFixtureGeneratorTests
{
    private const int RandomNumber = 50;

    [Test]
    public async Task Generar_TodosLosEscenarios()
    {
        var generated = new List<string>();

        foreach (var scenario in BuildScenarios())
        {
            var sut = PreflopTestCaseBuilder.Create(
                new FixedRangeStore(scenario.Table),
                new FixedRandomProvider(scenario.RandomNumber));

            var response = await sut.Execute(scenario.Request);
            var action = response.ResponseAction.Action;

            Assert.That(action, Is.Not.Null.And.Not.Empty, scenario.Id);
            Assert.That(action, Is.Not.EqualTo("None"), scenario.Id);

            var declared = scenario.Table.Positions?
                .SelectMany(position => position.Hands)
                .Select(hand => hand.Action)
                .ToHashSet() ?? [];

            Assert.That(declared, Does.Contain(action),
                $"{scenario.Id}: la tabla no declara '{action}' (posible secuencia sin match)");

            var fixture = scenario with { ExpectedAction = action! };
            PreflopFixtureStore.Save(fixture);
            generated.Add($"{fixture.Id} -> {action}");
        }

        TestContext.Progress.WriteLine(string.Join(Environment.NewLine, generated));
        Assert.That(generated, Has.Count.EqualTo(7));
    }

    private static IReadOnlyList<PreflopFixture> BuildScenarios()
    {
        return
        [
            new PreflopFixture(
                "open-raise-aks",
                CreateRequest(TablePosition.Button, "AKs", new Dictionary<TablePosition, decimal>()),
                CreateTable("fx-open-raise-aks", "Button", hands: [new Hand("AK", true, "Raise", 100)]),
                RandomNumber,
                string.Empty),

            new PreflopFixture(
                "open-raise-72o",
                CreateRequest(TablePosition.Button, "72o", new Dictionary<TablePosition, decimal>()),
                CreateTable("fx-open-raise-72o", "Button", hands: [new Hand("72", false, "Fold", 100)]),
                RandomNumber,
                string.Empty),

            new PreflopFixture(
                "raise-over-limper-aks",
                CreateRequest(TablePosition.Button, "AKs", new Dictionary<TablePosition, decimal> { [TablePosition.Middle] = 1m }),
                CreateTable("fx-raise-over-limper", "Button", limper: "Middle", hands: [new Hand("AK", true, "Raise", 100)]),
                RandomNumber,
                string.Empty),

            new PreflopFixture(
                "three-bet-qq",
                CreateRequest(TablePosition.Button, "QQ", new Dictionary<TablePosition, decimal> { [TablePosition.Middle] = 3m }),
                CreateTable("fx-three-bet", "Button", openRaiser: "Middle", hands: [new Hand("QQ", null, "Raise", 100)]),
                RandomNumber,
                string.Empty),

            new PreflopFixture(
                "cold-four-bet-ako",
                CreateRequest(TablePosition.Button, "AKo", new Dictionary<TablePosition, decimal>
                {
                    [TablePosition.Middle] = 3m,
                    [TablePosition.CutOff] = 9m
                }),
                CreateTable("fx-cold-four-bet", "Button", openRaiser: "Middle", threeBetPosition: "CutOff",
                    hands: [new Hand("AK", false, "Raise", 100)]),
                RandomNumber,
                string.Empty),

            new PreflopFixture(
                "squeeze-tt",
                CreateRequest(TablePosition.Button, "TT", new Dictionary<TablePosition, decimal>
                {
                    [TablePosition.Middle] = 3m,
                    [TablePosition.CutOff] = 3m
                }),
                CreateTable("fx-squeeze", "Button", openRaiser: "Middle", caller: "CutOff",
                    hands: [new Hand("TT", null, "Raise", 100)]),
                RandomNumber,
                string.Empty),

            new PreflopFixture(
                "second-action-vs-three-bet",
                CreateRequest(
                    TablePosition.Button,
                    "AKo",
                    new Dictionary<TablePosition, decimal> { [TablePosition.Middle] = 3m },
                    isSecondAction: true),
                CreateTable("fx-second-action", "Button", openRaiser: "Button", threeBetPosition: "Middle",
                    hands: [new Hand("AK", false, "Raise", 100)]),
                RandomNumber,
                string.Empty)
        ];
    }

    private static SetPreflopActionUseCaseRequest CreateRequest(
        TablePosition heroPosition,
        string hand,
        Dictionary<TablePosition, decimal> tableBets,
        bool isSecondAction = false)
    {
        var state = new PlayerGameState
        {
            Position = heroPosition,
            PotSize = 3m,
            HeroStack = 100m,
            IsInPosition = heroPosition is TablePosition.Button or TablePosition.CutOff,
            HandSituation = isSecondAction ? HandSituation.OpenRaise : HandSituation.None
        };

        ApplyHand(state, hand);

        foreach (var (position, bet) in tableBets)
        {
            state.Players.Add(new Player
            {
                Name = position.ToString(),
                Position = position,
                Active = true,
                Bet = bet,
                Stack = 100m
            });
        }

        return new SetPreflopActionUseCaseRequest
        {
            ResponseAction = new ResponseAction
            {
                Action = isSecondAction ? "Raise" : null,
                IsSecondAction = isSecondAction
            },
            PlayerState = state,
            PreflopHeroPosition = tableBets.Count == 0
                ? new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>()
                : new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>
                {
                    [heroPosition] = new(tableBets)
                }
        };
    }

    private static void ApplyHand(PlayerGameState state, string hand)
    {
        state.HoleCard1Face = hand[0].ToString();
        state.HoleCard2Face = hand[1].ToString();
        state.HoleCard1Rank = (int)ToRank(hand[0]);
        state.HoleCard2Rank = (int)ToRank(hand[1]);

        var suited = hand.Length > 2 && hand[2] == 's';
        state.HoleCard1Suit = (int)Suit.Spades;
        state.HoleCard2Suit = suited ? (int)Suit.Spades : (int)Suit.Hearts;
    }

    private static Rank ToRank(char value) => value switch
    {
        'A' => Rank.Ace,
        'K' => Rank.King,
        'Q' => Rank.Queen,
        'J' => Rank.Jack,
        'T' => Rank.Ten,
        '9' => Rank.Nine,
        '8' => Rank.Eight,
        '7' => Rank.Seven,
        '6' => Rank.Six,
        '5' => Rank.Five,
        '4' => Rank.Four,
        '3' => Rank.Three,
        '2' => Rank.Two,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Carta no soportada por el generador")
    };

    private static TableDTO CreateTable(
        string name,
        string heroPosition,
        string? openRaiser = null,
        string? threeBetPosition = null,
        string? limper = null,
        string? caller = null,
        List<Hand>? hands = null)
    {
        return new TableDTO
        {
            Name = name,
            Positions =
            [
                new PlayerActionSequence(
                    Name: name,
                    HeroPosition: heroPosition,
                    OpenRaiser: openRaiser,
                    ThreeBetPosition: threeBetPosition,
                    Limper: limper,
                    Caller: caller,
                    Squeezer: null,
                    BetSize: null,
                    IsGreater: null,
                    RaiserFolds: null,
                    Hands: hands ?? [])
            ]
        };
    }
}
