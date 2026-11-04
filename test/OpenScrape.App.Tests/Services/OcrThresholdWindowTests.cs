using OpenScrape.App.Services;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// Los umbrales de OCR viajan como fraccion 0..1 (ProcessBitmap hace porcentaje * 255),
/// pero las lecturas alternativas se calculaban sumando y restando 20, como si fueran
/// 0..255. Con 0.80 el intento "lower" quedaba en 0 (todo blanco) y el "higher" en 20.8
/// (corte imposible), asi que las dos pasadas de Tesseract no aportaban nada.
/// </summary>
[TestFixture]
public class OcrThresholdWindowTests
{
    [Test]
    public void Around_DevuelveLaVentanaEnLaMismaEscalaQueElUmbral()
    {
        var (lower, higher) = OcrThresholdWindow.Around(0.80);

        Assert.Multiple(() =>
        {
            Assert.That(lower, Is.EqualTo(0.72).Within(1e-9));
            Assert.That(higher, Is.EqualTo(0.88).Within(1e-9));
        });
    }

    [Test]
    public void Around_EnElPiso_RecortaElMenorACero()
    {
        var (lower, higher) = OcrThresholdWindow.Around(0.05);

        Assert.Multiple(() =>
        {
            Assert.That(lower, Is.EqualTo(0d));
            Assert.That(higher, Is.EqualTo(0.13).Within(1e-9));
        });
    }

    [Test]
    public void Around_EnElTecho_RecortaElMayorAUno()
    {
        var (lower, higher) = OcrThresholdWindow.Around(0.98);

        Assert.Multiple(() =>
        {
            Assert.That(lower, Is.EqualTo(0.90).Within(1e-9));
            Assert.That(higher, Is.EqualTo(1d));
        });
    }

    [Test]
    public void Around_SiempreQuedaDentroDeCeroYUno()
    {
        foreach (var umbral in new[] { 0d, 0.01, 0.30, 0.51, 0.85, 0.87, 0.99, 1d })
        {
            var (lower, higher) = OcrThresholdWindow.Around(umbral);

            Assert.Multiple(() =>
            {
                Assert.That(lower, Is.GreaterThanOrEqualTo(0d), $"lower fuera de rango para {umbral}");
                Assert.That(higher, Is.LessThanOrEqualTo(1d), $"higher fuera de rango para {umbral}");
            });
        }
    }

    [Test]
    public void LaLecturaAlternativa_NoUsaOffsetsDeEscalaDoscientosCincuentaYCinco()
    {
        var source = RepoFile("src", "OpenScrape.App", "Services", "OcrService.cs");

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Not.Contain("umbral - 20"),
                "El umbral es una fraccion 0..1: restarle 20 lo deja en cero.");
            Assert.That(source, Does.Not.Contain("umbral + 20"),
                "El umbral es una fraccion 0..1: sumarle 20 da un corte imposible.");
        });
    }

    private static string RepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        var relative = Path.Combine(parts);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relative);

            if (File.Exists(candidate))
                return File.ReadAllText(candidate);

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"No se encontro {relative} desde el directorio de tests.");
    }
}
