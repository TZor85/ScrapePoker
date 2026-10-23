using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Interfaces;
using OpenScrape.Domain.ValueObjects;
using OpenScrape.Features.ActionScenario;
using OpenScrape.Features.ActionScenario.Get;

namespace OpenScrape.App.Tests;

[TestFixture]
public class PreflopRangeStoreTests
{
    private sealed class FakeRangeStore : IPreflopRangeStore
    {
        private readonly TableDTO? _table;

        public FakeRangeStore(TableDTO? table)
        {
            _table = table;
        }

        public string? LastSituation { get; private set; }

        public Task<TableDTO?> GetAsync(string situation, CancellationToken cancellationToken = default)
        {
            LastSituation = situation;
            return Task.FromResult(_table);
        }
    }

    [Test]
    public async Task ExecuteAsync_ConTablaEnElStore_DevuelveLaAccionDeLaTabla()
    {
        // Arrange
        var table = new TableDTO
        {
            Name = "OpenRaise",
            Positions =
            [
                new PlayerActionSequence(
                    Name: "OpenRaise",
                    HeroPosition: TablePosition.Button.GetDescription(),
                    OpenRaiser: null,
                    ThreeBetPosition: null,
                    Limper: null,
                    Caller: null,
                    Squeezer: null,
                    BetSize: null,
                    IsGreater: null,
                    RaiserFolds: null,
                    Hands: [new Hand("AK", true, "Raise", 100)])
            ]
        };
        var store = new FakeRangeStore(table);
        var sut = new GetActionScenario(store);

        // Act
        var action = await sut.ExecuteAsync(
            GameSituation.OpenRaise,
            new ActionScenarioRequest { HeroPosition = TablePosition.Button, HandName = "AK", Suited = true });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(action, Is.EqualTo("Raise"));
            Assert.That(store.LastSituation, Is.EqualTo(GameSituation.OpenRaise.GetDescription()));
        });
    }

    [Test]
    public void ExecuteAsync_SinTabla_Lanza()
    {
        // Arrange
        var sut = new GetActionScenario(new FakeRangeStore(null));

        // Act
        var ex = Assert.ThrowsAsync<Exception>(async () => await sut.ExecuteAsync(
            GameSituation.OpenRaise,
            new ActionScenarioRequest()));

        // Assert
        Assert.That(ex!.Message, Does.Contain("Table not found"));
    }
}
