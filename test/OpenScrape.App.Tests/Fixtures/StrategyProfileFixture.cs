using System.Text.Json;
using System.Text.Json.Serialization;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Carga el <see cref="StrategyProfile"/> con el que se generaron los fixtures.
/// Si el snapshot no existe todavía, lo toma de la configuración de la app para que
/// los fixtures reflejen el perfil real y no los valores por defecto de la clase.
/// </summary>
internal static class StrategyProfileFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static StrategyProfile LoadOrCreate()
    {
        var path = File.Exists(FixturePaths.StrategyProfile)
            ? FixturePaths.StrategyProfile
            : FixturePaths.SourceStrategyProfile;

        if (File.Exists(path))
            return JsonSerializer.Deserialize<StrategyProfile>(
                File.ReadAllText(path), JsonOptions)!;

        return LoadFromAppSettings();
    }

    public static void Save(StrategyProfile profile)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FixturePaths.SourceStrategyProfile)!);
        File.WriteAllText(FixturePaths.SourceStrategyProfile, JsonSerializer.Serialize(profile, JsonOptions));
    }

    /// <summary>Perfil configurado en producción (sección StrategyProfile de appsettings.json).</summary>
    public static StrategyProfile LoadFromProduction() => LoadFromAppSettings();

    /// <summary>Perfil congelado que usan los fixtures.</summary>
    public static StrategyProfile LoadSnapshot()
    {
        if (!File.Exists(FixturePaths.SourceStrategyProfile))
            throw new FileNotFoundException(
                "No existe el snapshot de estrategia versionado.", FixturePaths.SourceStrategyProfile);

        return JsonSerializer.Deserialize<StrategyProfile>(
            File.ReadAllText(FixturePaths.SourceStrategyProfile), JsonOptions)!;
    }

    public static string Serialize(StrategyProfile profile) => JsonSerializer.Serialize(profile, JsonOptions);

    private static StrategyProfile LoadFromAppSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            throw new FileNotFoundException(
                "No se encontró appsettings.json en el output de los tests. Revisar el ItemGroup de copia.", path);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var section = document.RootElement.GetProperty("StrategyProfile").GetRawText();
        return JsonSerializer.Deserialize<StrategyProfile>(section, JsonOptions)
            ?? throw new InvalidOperationException("No se pudo deserializar la sección StrategyProfile.");
    }
}
