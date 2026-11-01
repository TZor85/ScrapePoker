using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Tests.Telemetry;

/// <summary>
/// La pestaña Métricas dibuja una fila por cada categoría de <c>DisplayOrder</c>.
/// Si una categoría no se mide en ningún lado, la fila queda vacía para siempre y
/// hace parecer que el pipeline no la reporta.
/// </summary>
[TestFixture]
public class TelemetryCategoriesTests
{
    private static readonly string[] CategoriasSinProductor =
    {
        "Decision.Equity",
        "Decision.Texture",
        "Decision.Profile",
        "Decision.DecisionService",
        "Decision.Sizing",
        "Persistence.SaveHand",
    };

    [Test]
    public void DisplayOrder_NoListaCategoriasQueNadieMide()
    {
        foreach (var categoria in CategoriasSinProductor)
        {
            Assert.That(
                TelemetryCategories.DisplayOrder,
                Does.Not.Contain(categoria),
                $"{categoria} no tiene ningún productor en el cliente y quedaba como fila vacía.");
        }
    }

    [Test]
    public void DisplayOrder_MantieneLasCategoriasQueSiSeMiden()
    {
        var vivas = new[]
        {
            TelemetryCategories.CycleTotal,
            TelemetryCategories.CaptureScreenshot,
            TelemetryCategories.OcrCards,
            TelemetryCategories.OcrBets,
            TelemetryCategories.OcrStacks,
            TelemetryCategories.OcrHandNumber,
            TelemetryCategories.OcrPlayerNames,
            TelemetryCategories.LayoutDealer,
            TelemetryCategories.LayoutPositions,
            TelemetryCategories.DecisionTotal,
            TelemetryCategories.OverlayRender,
        };

        foreach (var categoria in vivas)
            Assert.That(TelemetryCategories.DisplayOrder, Does.Contain(categoria), $"{categoria} sí se mide.");
    }
}
