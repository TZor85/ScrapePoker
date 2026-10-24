using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Fixtures;

[TestFixture]
public class PostflopFixtureReplayTests
{
    [Test]
    public void Replay_CadaEscenario_CoincideConLaDecisionEsperada()
    {
        // Arrange
        var profile = StrategyProfileFixture.LoadOrCreate();
        var fixtures = PostflopFixtureStore.LoadAll();

        Assert.That(fixtures, Is.Not.Empty, "Faltan fixtures postflop; correr el generador [Explicit].");

        // Act / Assert
        Assert.Multiple(() =>
        {
            foreach (var fixture in fixtures)
            {
                var street = Enum.Parse<BoardPosition>(fixture.Street, ignoreCase: true);
                var snapshot = PostflopDecisionRunner.RunStreet(
                    profile,
                    fixture.State,
                    fixture.MaxBet,
                    fixture.MonteCarloSeed,
                    street);

                Assert.That(snapshot.Action, Is.EqualTo(fixture.ExpectedAction), fixture.Id);
                Assert.That(snapshot.Equity, Is.EqualTo(fixture.ExpectedEquity).Within(0.1), fixture.Id);
                Assert.That(snapshot.Reason, Is.EqualTo(fixture.ExpectedReason), fixture.Id);
                Assert.That(snapshot.BoardTexture, Is.EqualTo(fixture.ExpectedBoardTexture), fixture.Id);
                Assert.That(snapshot.IsBluff, Is.EqualTo(fixture.ExpectedIsBluff), fixture.Id);
                Assert.That(snapshot.IsBarrel, Is.EqualTo(fixture.ExpectedIsBarrel), fixture.Id);
                Assert.That(snapshot.IsCheckRaise, Is.EqualTo(fixture.ExpectedIsCheckRaise), fixture.Id);
            }
        });
    }
}
