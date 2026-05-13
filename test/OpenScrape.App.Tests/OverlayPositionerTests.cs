using Microsoft.Extensions.Options;

using OpenScrape.App.Services;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del OverlayPositioner extraído de FrmMain.CalculateOverlayPosition
/// (extract-frmmain-testable-logic Fase 3).
/// </summary>
[TestFixture]
public class OverlayPositionerTests
{
    private static OverlayPositioner Make(double hOffsetPct = 0.0, int vOffset = 100)
    {
        var options = Options.Create(new OverlayConfig
        {
            HorizontalOffsetPercent = hOffsetPct,
            VerticalOffset = vOffset,
        });
        return new OverlayPositioner(options);
    }

    [Test]
    public void Calculate_VentanaEstandarSinOffsetHorizontal_CentroLaBasica()
    {
        var positioner = Make(hOffsetPct: 0.0, vOffset: 100);
        // Ventana 1920x1080, centro = 960, overlay 400, half=200 → x = 760
        // y = 1080 - 100 = 980
        var result = positioner.Calculate(0, 1920, 1080, 400);

        Assert.Multiple(() =>
        {
            Assert.That(result.X, Is.EqualTo(760));
            Assert.That(result.Y, Is.EqualTo(980));
        });
    }

    [Test]
    public void Calculate_OffsetHorizontal5Porciento_DesplazaX()
    {
        var positioner = Make(hOffsetPct: 0.05);
        // windowWidth=1920, horizontalOffset=96
        // x = 960 - 200 - 96 = 664
        var result = positioner.Calculate(0, 1920, 1080, 400);

        Assert.That(result.X, Is.EqualTo(664));
    }

    [Test]
    public void Calculate_OffsetVertical200_AjustaY()
    {
        var positioner = Make(vOffset: 200);
        var result = positioner.Calculate(0, 1920, 1080, 400);

        Assert.That(result.Y, Is.EqualTo(880)); // 1080 - 200
    }

    [Test]
    public void Calculate_OverlayMasAnchoQueVentana_PermiteXNegativoSinClamping()
    {
        var positioner = Make(hOffsetPct: 0.0);
        // Ventana [0,100], overlay 400, centro=50, half=200 → x = -150
        var result = positioner.Calculate(0, 100, 500, 400);

        Assert.That(result.X, Is.EqualTo(-150));
    }

    [Test]
    public void Calculate_VentanaDesplazada_RespetaOrigen()
    {
        var positioner = Make(hOffsetPct: 0.0, vOffset: 50);
        // Ventana [100, 1100] (width=1000), centro=600, overlay 200 → x=500
        // y = 800 - 50 = 750
        var result = positioner.Calculate(100, 1100, 800, 200);

        Assert.Multiple(() =>
        {
            Assert.That(result.X, Is.EqualTo(500));
            Assert.That(result.Y, Is.EqualTo(750));
        });
    }

    [Test]
    public void Calculate_HorizontalOffsetPercentCero_NoAlteraX()
    {
        var positioner = Make(hOffsetPct: 0.0);
        // Comparar con cualquier x esperado derivado de la aritmética sin offset
        var result = positioner.Calculate(0, 1000, 600, 200);
        // windowWidth=1000, center=500, overlay half=100 → x=400
        Assert.That(result.X, Is.EqualTo(400));
    }
}
