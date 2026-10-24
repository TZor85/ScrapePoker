using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Services;
using OpenScrape.App.Services.Http;
using OpenScrape.App.Tests.Api;
using OpenScrape.App.Tests.Fixtures;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests;

/// <summary>
/// Paridad por HTTP: el mismo coordinador con el motor local y con el motor remoto
/// debe producir exactamente la decisión congelada en los fixtures.
/// </summary>
[TestFixture]
public class ParidadHttpTests
{
    [Test]
    public async Task Preflop_LosOchoFixtures_CoincidenPorHttp()
    {
        var fixtures = PreflopFixtureStore.LoadAll();
        Assert.That(fixtures, Has.Count.EqualTo(8), "Faltan fixtures preflop.");

        foreach (var fixture in fixtures)
        {
            using var factory = new ApiFactory(fixture.Table, fixture.RandomNumber);
            using var client = factory.CreateClient();
            var engine = new HttpDecisionEngine(client, NullLogger<HttpDecisionEngine>.Instance);

            var request = PlayerStateMapper.MapPreflop(
                fixture.Request.PlayerState,
                fixture.Request.ResponseAction.IsSecondAction,
                villain: null);

            var result = await engine.DecideAsync(request);

            Assert.That(result.Action, Is.EqualTo(fixture.ExpectedAction), fixture.Id);
        }
    }

    [Test]
    public async Task Postflop_LosSieteFixtures_CoincidenPorHttp()
    {
        var profile = StrategyProfileFixture.LoadOrCreate();
        var fixtures = PostflopFixtureStore.LoadAll();
        Assert.That(fixtures, Has.Count.EqualTo(7), "Faltan fixtures postflop.");

        foreach (var fixture in fixtures)
        {
            using var factory = new ApiFactory(
                FixtureTables.Empty(),
                randomNumber: 0,
                monteCarloSeed: fixture.MonteCarloSeed);
            using var client = factory.CreateClient();
            var engine = new HttpDecisionEngine(client, NullLogger<HttpDecisionEngine>.Instance);
            var street = Enum.Parse<BoardPosition>(fixture.Street, ignoreCase: true);

            var snapshot = await PostflopDecisionRunner.RunStreet(
                profile,
                fixture.State,
                fixture.MaxBet,
                fixture.MonteCarloSeed,
                street,
                engine: engine);

            Assert.That(snapshot.Action, Is.EqualTo(fixture.ExpectedAction), fixture.Id);
            Assert.That(snapshot.Equity, Is.EqualTo(fixture.ExpectedEquity).Within(0.1), fixture.Id);
            Assert.That(snapshot.Reason, Is.EqualTo(fixture.ExpectedReason), fixture.Id);
            Assert.That(snapshot.BoardTexture, Is.EqualTo(fixture.ExpectedBoardTexture), fixture.Id);
            Assert.That(snapshot.IsBluff, Is.EqualTo(fixture.ExpectedIsBluff), fixture.Id);
            Assert.That(snapshot.IsBarrel, Is.EqualTo(fixture.ExpectedIsBarrel), fixture.Id);
            Assert.That(snapshot.IsCheckRaise, Is.EqualTo(fixture.ExpectedIsCheckRaise), fixture.Id);
        }
    }
}
