using System.Text.Json;
using System.Text.Json.Serialization;
using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Contracts;

[TestFixture]
public class DecisionContractSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Test]
    public void Serialize_RequestPreflop_UsaPropiedadesCamelCaseYEnumsComoTexto()
    {
        var request = new DecisionRequest
        {
            Street = DecisionStreet.Preflop,
            HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
            HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
            PotSize = 3m,
            HeroStack = 100m,
            HeroPosition = TablePosition.Button,
            HandSituation = HandSituation.None
        };

        var json = JsonSerializer.Serialize(request, Options);

        Assert.That(json, Does.Contain("\"street\":\"preflop\""));
        Assert.That(json, Does.Contain("\"handSituation\":\"none\""));
        Assert.That(json, Does.Contain("\"heroCard1\":{\"rank\":"));
    }

    [Test]
    public void RoundTrip_ResultConDetalle_ConservaTodasLasPropiedades()
    {
        var original = new DecisionResult
        {
            Action = "Bet 33%",
            Equity = 54.2,
            RawEquity = 61.0,
            PotOdds = 25.0,
            BetSize = 12.5,
            Reason = "top pair",
            BoardTexture = "Dry",
            BoardWetnessScore = 0.42,
            EVWithFoldEquity = 3.75,
            IsBluff = false,
            IsBarrel = true,
            DrawTypes = ["Flush Draw"],
            EffectiveOuts = 9,
            HeroHandRank = "OnePair",
            KickerStrength = "Strong",
            PairClassification = "TopPair"
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionResult>(json, Options);

        Assert.That(restored!.Action, Is.EqualTo(original.Action));
        Assert.That(restored.Equity, Is.EqualTo(original.Equity));
        Assert.That(restored.RawEquity, Is.EqualTo(original.RawEquity));
        Assert.That(restored.BetSize, Is.EqualTo(original.BetSize));
        Assert.That(restored.BoardWetnessScore, Is.EqualTo(original.BoardWetnessScore));
        Assert.That(restored.EVWithFoldEquity, Is.EqualTo(original.EVWithFoldEquity));
        Assert.That(restored.DrawTypes, Is.EquivalentTo(original.DrawTypes));
        Assert.That(restored.HeroHandRank, Is.EqualTo(original.HeroHandRank));
    }

    [Test]
    public void Deserialize_RequestSinBoard_DejaListaVacia()
    {
        const string json = """
        { "street": "flop", "heroCard1": {"rank": 14, "suit": 3},
          "heroCard2": {"rank": 13, "suit": 3}, "potSize": 6, "heroStack": 94 }
        """;

        var request = JsonSerializer.Deserialize<DecisionRequest>(json, Options);

        Assert.That(request!.Board, Is.Empty);
        Assert.That(request.Context, Is.Not.Null);
    }
}
