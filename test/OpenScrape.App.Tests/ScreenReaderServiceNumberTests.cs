using System.Drawing;
using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests de la lectura y normalización de importes en ScreenReaderService.
/// El OCR devuelve los importes tal como los muestra el cliente de póker: con coma
/// decimal. En un Windows en inglés la coma es separador de miles, así que sin una
/// cultura fija el importe se lee cien veces más grande. Toda la clase corre en en-US
/// a propósito: los resultados deben ser idénticos a los de es-AR.
/// </summary>
[TestFixture]
[SetCulture("en-US")]
public class ScreenReaderServiceNumberTests
{
    #region NormalizeBetValue

    [Test]
    public void NormalizeBetValue_Recupera_Separador_Decimal_Perdido_En_Cultura_Inglesa()
    {
        // "593" sin separador y >= 300: el OCR perdió la coma, la apuesta es 5,93
        Assert.That(CreateService().NormalizeBetValue(593m), Is.EqualTo(5.93m));
    }

    [Test]
    public void NormalizeBetValue_Corrige_Artefacto_Ocho_Inicial_En_Cultura_Inglesa()
    {
        // "815,50": el 8 inicial es un artefacto del OCR, la apuesta es 15,50
        Assert.That(CreateService().NormalizeBetValue(815.50m), Is.EqualTo(15.50m));
    }

    [Test]
    public void NormalizeBetValue_Respeta_Importe_Con_Decimales_En_Cultura_Inglesa()
    {
        Assert.That(CreateService().NormalizeBetValue(1525.25m), Is.EqualTo(1525.25m));
    }

    #endregion

    #region NormalizeStackValue

    [Test]
    public void NormalizeStackValue_Recupera_Separador_Decimal_Perdido_En_Cultura_Inglesa()
    {
        // "1500" sin separador: el stack no supera los 500 BB, es 15,00
        Assert.That(CreateService().NormalizeStackValue(1500m), Is.EqualTo(15.00m));
    }

    [Test]
    public void NormalizeStackValue_Corrige_Artefacto_Ocho_Inicial_En_Cultura_Inglesa()
    {
        // "812,50" → 12,50
        Assert.That(CreateService().NormalizeStackValue(812.50m), Is.EqualTo(12.50m));
    }

    #endregion

    #region Lectura OCR

    [Test]
    public void ReadBetValue_Parsea_Coma_Decimal_En_Cultura_Inglesa()
    {
        using var screenshot = CreateScreenshot();

        var bet = CreateService(new OcrTextStub("15,25"))
            .ReadBetValue(screenshot, 0, 0, 10, 5, 0.5, 0.1, false);

        Assert.That(bet, Is.EqualTo(15.25m));
    }

    [Test]
    public void ReadStackValue_Parsea_Coma_Decimal_En_Cultura_Inglesa()
    {
        using var screenshot = CreateScreenshot();

        var stack = CreateService(new OcrTextStub("15,25"))
            .ReadStackValue(screenshot, 0, 0, 10, 5, 0.5, 0.1, true);

        Assert.That(stack, Is.EqualTo(15.25m));
    }

    #endregion

    private static ScreenReaderService CreateService(IOcrService? ocr = null) =>
        new(ocr ?? new OcrTextStub("0"),
            NullLogger<ScreenReaderService>.Instance,
            new MetricsCollector(NullLogger<MetricsCollector>.Instance));

    private static Bitmap CreateScreenshot() => new(20, 10);

    private sealed class OcrTextStub(string text) : IOcrService
    {
        public void ClearCache() { }

        public OcrResult ExtractTextFromRegionAndDebug(Image sourceImage, int x, int y, int width, int height,
            double umbral = 0, bool onlyNumber = false) =>
            new() { Text = text, Confidence = 0.9f };
    }
}
