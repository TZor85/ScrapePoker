using System.Text.Json;
using NUnit.Framework;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class HandRecordTelemetryPersistenceTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = false,
        PropertyNamingPolicy = null,
    };

    [Test]
    public void HandRecord_SinTelemetry_Roundtrip()
    {
        var hr = new HandRecord { HandNumber = 1 };
        var json = JsonSerializer.Serialize(hr, Options);
        var back = JsonSerializer.Deserialize<HandRecord>(json, Options);

        Assert.That(back, Is.Not.Null);
        Assert.That(back!.Telemetry, Is.Null);
    }

    [Test]
    public void HandRecord_ConTelemetry_Roundtrip()
    {
        var hr = new HandRecord
        {
            HandNumber = 1,
            Telemetry = new TelemetryAggregate(
                HandId: "hand-1",
                CapturedAt: new DateTime(2026, 4, 23, 12, 0, 0, DateTimeKind.Utc),
                Phases: new Dictionary<string, CategoryStats>
                {
                    ["OCR.Cards"] = new CategoryStats(P50Ms: 10.0, P95Ms: 25.0, MaxMs: 40.0, Count: 50),
                    ["Decision.Total"] = new CategoryStats(P50Ms: 120.0, P95Ms: 180.0, MaxMs: 210.0, Count: 3),
                }),
        };

        var json = JsonSerializer.Serialize(hr, Options);
        var back = JsonSerializer.Deserialize<HandRecord>(json, Options);

        Assert.That(back, Is.Not.Null);
        Assert.That(back!.Telemetry, Is.Not.Null);
        Assert.That(back.Telemetry!.HandId, Is.EqualTo("hand-1"));
        Assert.That(back.Telemetry.Phases, Has.Count.EqualTo(2));
        Assert.That(back.Telemetry.Phases["OCR.Cards"].Count, Is.EqualTo(50));
        Assert.That(back.Telemetry.Phases["Decision.Total"].P95Ms, Is.EqualTo(180.0));
    }

    [Test]
    public void HandRecord_JsonAntiguo_SinCampoTelemetry_Deserializa()
    {
        // JSON que representa una mano persistida antes de añadir Telemetry
        const string legacyJson = """
        {
            "Id": "abc",
            "GameSessionId": "sess-1",
            "HandNumber": 42,
            "HeroCard1": "Ah",
            "HeroCard2": "Kh",
            "HeroStackStart": 100.0,
            "HeroStackEnd": 105.0,
            "FlopCards": [],
            "Decisions": [],
            "LastStreetPlayed": 0,
            "NumOpponents": 1,
            "Result": 0,
            "Situation": 0
        }
        """;

        var back = JsonSerializer.Deserialize<HandRecord>(legacyJson, Options);

        Assert.That(back, Is.Not.Null);
        Assert.That(back!.Telemetry, Is.Null, "Telemetry debe deserializar como null en manos antiguas");
        Assert.That(back.HandNumber, Is.EqualTo(42));
    }
}
