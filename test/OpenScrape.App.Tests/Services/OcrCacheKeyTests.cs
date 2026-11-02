using OpenScrape.App.Services;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// La caché de OCR estaba indexada sólo por el recorte (dHash). Como `ReadPlayerName`
/// lee el mismo recorte con dos umbrales, la segunda lectura recibía el resultado de
/// la primera y el umbral inactivo nunca se usaba: los nombres en gris (jugador
/// retirado) siempre volvían vacíos.
/// </summary>
[TestFixture]
public class OcrCacheKeyTests
{
    [Test]
    public void MismoRecorteConUmbralesDistintos_NoComparteClave()
    {
        var alto = OcrCacheKey.From(1234UL, 0.80, onlyNumber: false);
        var bajo = OcrCacheKey.From(1234UL, 0.30, onlyNumber: false);

        Assert.That(alto, Is.Not.EqualTo(bajo),
            "Dos lecturas del mismo recorte con umbrales distintos deben tener claves distintas.");
    }

    [Test]
    public void MismoRecorteConDistintoOnlyNumber_NoComparteClave()
    {
        var texto = OcrCacheKey.From(1234UL, 0.85, onlyNumber: false);
        var numero = OcrCacheKey.From(1234UL, 0.85, onlyNumber: true);

        Assert.That(texto, Is.Not.EqualTo(numero));
    }

    [Test]
    public void MismoRecorte_Y_MismoUmbral_ComparteClave()
    {
        var primera = OcrCacheKey.From(1234UL, 0.80, onlyNumber: false);
        var segunda = OcrCacheKey.From(1234UL, 0.80, onlyNumber: false);

        Assert.That(primera, Is.EqualTo(segunda));
    }

    [Test]
    public void ElUmbralSeCuantizaA255Niveles()
    {
        var clave = OcrCacheKey.From(99UL, 0.80, onlyNumber: false);

        Assert.That(clave.Threshold, Is.EqualTo(204));
    }
}
