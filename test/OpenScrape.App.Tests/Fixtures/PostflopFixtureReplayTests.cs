namespace OpenScrape.App.Tests.Fixtures;

[TestFixture]
public class PostflopFixtureReplayTests
{
    [Test]
    public void Replay_CadaEscenarioFlop_CoincideConLaAccionEsperada()
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
                var (action, equity) = PostflopDecisionRunner.Run(
                    profile,
                    fixture.State,
                    fixture.MaxBet,
                    fixture.MonteCarloSeed);

                Assert.That(action, Is.EqualTo(fixture.ExpectedAction), fixture.Id);
                Assert.That(equity, Is.EqualTo(fixture.ExpectedEquity).Within(0.1), fixture.Id);
            }
        });
    }
}
