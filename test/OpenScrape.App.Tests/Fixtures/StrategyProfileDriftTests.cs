using System.Text.Json.Nodes;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// El snapshot de estrategia con el que se generaron los fixtures tiene que seguir
/// siendo el mismo que usa producción. Si divergen, el replay valida decisiones
/// contra un perfil que ya nadie usa.
/// </summary>
[TestFixture]
public class StrategyProfileDriftTests
{
    [Test]
    public void Snapshot_CoincideConLaConfiguracionDeProduccion()
    {
        // Arrange
        var production = WithoutVolatileFields(
            StrategyProfileFixture.Serialize(StrategyProfileFixture.LoadFromProduction()));
        var snapshot = WithoutVolatileFields(
            StrategyProfileFixture.Serialize(StrategyProfileFixture.LoadSnapshot()));

        var expected = production.ToJsonString();
        var actual = snapshot.ToJsonString();

        // Assert
        Assert.That(
            JsonNode.DeepEquals(snapshot, production),
            Is.True,
            "El snapshot de estrategia quedó desactualizado respecto de appsettings.json. " +
            "Regenerar los fixtures postflop después de aceptar el cambio de perfil. " +
            FirstDifference(expected, actual));
    }

    /// <summary>
    /// Id y CreatedAt se generan al construir la instancia y no vienen en appsettings.json,
    /// así que no forman parte del contrato de configuración.
    /// </summary>
    private static JsonObject WithoutVolatileFields(string json)
    {
        var node = JsonNode.Parse(json)!.AsObject();
        node.Remove("Id");
        node.Remove("CreatedAt");
        return node;
    }

    private static string FirstDifference(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var index = 0;
        while (index < expectedLines.Length && index < actualLines.Length &&
               expectedLines[index] == actualLines[index])
            index++;

        if (index >= expectedLines.Length && index >= actualLines.Length)
            return string.Empty;

        return $"Primera diferencia en la línea {index + 1}. " +
               $"Producción: {At(expectedLines, index)} | Snapshot: {At(actualLines, index)}";
    }

    private static string At(string[] lines, int index) =>
        index < lines.Length ? lines[index].Trim() : "<fin>";
}
