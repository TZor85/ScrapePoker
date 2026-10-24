using System.Text.Json;
using System.Text.Json.Serialization;
using OpenScrape.App.Aplication;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Interfaces;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Escenario preflop congelado: estado de entrada, tabla de rangos, número aleatorio
/// y acción que produjo el motor. La tabla viaja dentro del fixture para que el
/// replay no dependa de Postgres.
/// </summary>
internal sealed record PreflopFixture(
    string Id,
    SetPreflopActionUseCaseRequest Request,
    TableDTO Table,
    int RandomNumber,
    string ExpectedAction);

/// <summary>Store de fixtures preflop en JSON.</summary>
internal static class PreflopFixtureStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<PreflopFixture> LoadAll()
    {
        var directory = FixturePaths.Preflop;
        if (!Directory.Exists(directory))
            return [];

        return Directory
            .GetFiles(directory, "*.json")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => JsonSerializer.Deserialize<PreflopFixture>(File.ReadAllText(path), Options))
            .OfType<PreflopFixture>()
            .ToList();
    }

    public static void Save(PreflopFixture fixture)
    {
        var directory = FixturePaths.SourcePreflop;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{fixture.Id}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(fixture, Options));
    }
}

/// <summary>Store de rangos fijo: devuelve siempre la tabla del fixture.</summary>
internal sealed class FixedRangeStore(TableDTO table) : IPreflopRangeStore
{
    public Task<TableDTO?> GetAsync(string situation, CancellationToken cancellationToken = default)
        => Task.FromResult<TableDTO?>(table);
}

/// <summary>Proveedor aleatorio fijo: reproduce siempre el mismo número.</summary>
internal sealed class FixedRandomProvider(int value) : IRandomProvider
{
    public double NextDouble() => 0d;

    public int Next(int minValue, int maxValue) => value;
}
