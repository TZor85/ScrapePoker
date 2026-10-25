using OpenScrape.App.Aplication;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Reproduce cada fixture preflop contra el motor actual y compara la acción.
/// Si un fixture desaparece o no coincide, la paridad de la migración no está probada.
/// </summary>
[TestFixture]
public class PreflopFixtureReplayTests
{
    [Test]
    public async Task Replay_CadaFixture_DevuelveLaAccionEsperada()
    {
        var fixtures = PreflopFixtureStore.LoadAll();
        Assert.That(fixtures, Is.Not.Empty, "Faltan fixtures preflop; correr el generador [Explicit].");

        var failures = new List<string>();

        foreach (var fixture in fixtures)
        {
            var sut = PreflopTestCaseBuilder.Create(
                new FixedRangeStore(fixture.Table),
                new FixedRandomProvider(fixture.RandomNumber));

            SetPreflopActionUseCaseResponse response = await sut.Execute(fixture.Request);

            if (response.ResponseAction.Action != fixture.ExpectedAction)
            {
                failures.Add(
                    $"{fixture.Id}: esperado '{fixture.ExpectedAction}', obtenido '{response.ResponseAction.Action}'");
            }
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }
}
