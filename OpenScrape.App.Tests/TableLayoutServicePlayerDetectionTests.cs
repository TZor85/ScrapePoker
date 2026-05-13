using System.Drawing;

using Microsoft.Extensions.Logging.Abstractions;

using OpenScrape.App.Entities;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

using RegionValue = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Tests;

[TestFixture]
public class TableLayoutServicePlayerDetectionTests
{
    [Test]
    public void SetActivePlayer_P5PlayingDespuesDeEmpty_LimpiaEmptyYMarcaActivo()
    {
        var service = CreateService(out var cache);
        cache.Initialize(new[]
        {
            new RegionTableMap
            {
                Id = "Empty",
                Regions = new List<RegionValue>
                {
                    CreateRegion("Empty", "p5empty", 1, 1)
                }
            },
            new RegionTableMap
            {
                Id = "Playing",
                Regions = new List<RegionValue>
                {
                    CreateRegion("Playing", "p5playing", 2, 2)
                }
            }
        });

        using var bitmap = new Bitmap(10, 10);
        bitmap.SetPixel(1, 1, Color.FromArgb(0, 0, 14));
        bitmap.SetPixel(2, 2, Color.FromArgb(0, 0, 17));
        var state = new PlayerGameState();

        service.SetEmptyPlayer(bitmap, state);
        service.SetActivePlayer(bitmap, state);

        var p5 = state.Players.Single(p => p.Name == "P5");
        Assert.That(p5.Active, Is.True);
        Assert.That(p5.Empty, Is.False);
        Assert.That(p5.SitOut, Is.False);
    }

    [Test]
    public void ValidatePlayerStates_P5ConBet_SeConsideraJugadorActivo()
    {
        var service = CreateService(out _);
        var state = new PlayerGameState
        {
            Players = new List<Player>
            {
                new() { Name = "P0", ValuePosition = 0, Empty = false, SitOut = false, Active = true },
                new() { Name = "P5", ValuePosition = 5, Empty = true, SitOut = true, Active = false, Bet = 1m }
            }
        };

        service.ValidatePlayerStates(state);

        var p5 = state.Players.Single(p => p.Name == "P5");
        Assert.That(p5.Active, Is.True);
        Assert.That(p5.Empty, Is.False);
        Assert.That(p5.SitOut, Is.False);
        Assert.That(p5.HasFolded, Is.False);
    }

    private static TableLayoutService CreateService(out RegionLookupCache cache)
    {
        cache = new RegionLookupCache();
        return new TableLayoutService(
            cache,
            new CoordinateScaler(),
            new FakeScreenReaderService(),
            new FakeOpponentTracker(),
            NullLogger<TableLayoutService>.Instance,
            new MetricsCollector(NullLogger<MetricsCollector>.Instance));
    }

    private static RegionValue CreateRegion(string category, string name, int x, int y) =>
        new(category, name, x, y, 1, 1, null, true, null, null, null, null, null);

    private sealed class FakeScreenReaderService : IScreenReaderService
    {
        public string ReadPlayerName(Image screenshot, int x, int y, int w, int h, double umbral, double inactiveUmbral) => string.Empty;
        public decimal ReadBetValue(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber, int? playerNum = null) => 0;
        public decimal ReadStackValue(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber) => 0;
        public string ReadHandNumber(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber) => string.Empty;
        public string ReadText(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber) => string.Empty;
        public string ReadTextWithMultipleThresholds(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber) => string.Empty;
        public decimal NormalizeBetValue(decimal rawValue, decimal potSize = 0) => rawValue;
        public decimal NormalizeStackValue(decimal rawValue) => rawValue;
    }

    private sealed class FakeOpponentTracker : IOpponentTracker
    {
        public IReadOnlyDictionary<string, OpponentProfile> AllProfiles { get; } = new Dictionary<string, OpponentProfile>();

        public OpponentProfile GetProfile(string playerId) => new() { PlayerId = playerId };
        public void RecordHandPlayed(string playerId, TablePosition position = TablePosition.None) { }
        public void RecordVPIP(string playerId, TablePosition position = TablePosition.None) { }
        public void RecordPFR(string playerId, TablePosition position = TablePosition.None) { }
        public void RecordThreeBet(string playerId) { }
        public void RecordPostflopAction(string playerId, PostflopAction action, bool? isVillainInPosition = null) { }
        public void RecordCBetOpportunity(string playerId, bool didCBet) { }
        public void RecordFacedCBet(string playerId, bool folded) { }
        public double GetAdjustedFoldEquity(string playerId, double baseFoldEquity) => baseFoldEquity;
        public double GetFoldToBetPct(string playerId) => -1;
        public void TrackShowdownResult(string playerId, bool wentToSD, bool wonSD) { }
        public void TrackCheckRaise(string playerId, bool didCR, bool hadOpportunity) { }
        public void TrackDonkBet(string playerId, bool didDonk, bool hadOpportunity) { }
        public void TrackBarrel(string playerId, bool didBarrel) { }
        public void RegisterSeatAlias(string seatName, string alias) { }
        public string? ResolveName(string seatName) => null;
        public void Reset() { }
    }
}
