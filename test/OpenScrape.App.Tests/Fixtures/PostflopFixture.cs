using System.Text.Json;
using System.Text.Json.Serialization;
using OpenScrape.App.Entities;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Un escenario postflop congelado: el estado leído de la mesa y la decisión que
/// produjo el código en el momento de generar el fixture.
/// </summary>
internal sealed record PostflopFixture(
    string Id,
    string Street,
    PlayerGameState State,
    decimal MaxBet,
    string ExpectedAction,
    double ExpectedEquity,
    int MonteCarloSeed);

internal static class PostflopFixtureStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<PostflopFixture> LoadAll()
    {
        var directory = Directory.Exists(FixturePaths.Postflop) && Directory.GetFiles(FixturePaths.Postflop, "*.json").Length > 0
            ? FixturePaths.Postflop
            : FixturePaths.SourcePostflop;

        if (!Directory.Exists(directory))
            return [];

        return Directory.GetFiles(directory, "*.json")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => JsonSerializer.Deserialize<PostflopFixture>(File.ReadAllText(path), JsonOptions)!)
            .ToList();
    }

    public static void Save(PostflopFixture fixture)
    {
        Directory.CreateDirectory(FixturePaths.SourcePostflop);
        var path = Path.Combine(FixturePaths.SourcePostflop, $"{fixture.Id}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(fixture, JsonOptions));
    }
}
