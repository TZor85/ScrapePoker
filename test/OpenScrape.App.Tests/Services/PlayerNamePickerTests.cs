using OpenScrape.App.Services;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// `ReadPlayerName` desempataba las dos lecturas por longitud, y la lectura del umbral
/// alto suele salir más larga porque mete ruido del halo del texto. Medido con Tesseract
/// sobre las capturas reales: a 0.30 los cinco asientos se leen (incluidos los nombres
/// en gris de los jugadores retirados) y a 0.80 cuatro devuelven un punto.
/// </summary>
[TestFixture]
public class PlayerNamePickerTests
{
    [Test]
    public void AmbasLecturasIguales_DevuelveEsa()
    {
        var elegido = PlayerNamePicker.Choose("RastaFlow", "RastaFlow");

        Assert.That(elegido, Is.EqualTo("RastaFlow"));
    }

    [Test]
    public void SoloLaDelUmbralAltoTieneTexto_DevuelveEsa()
    {
        var elegido = PlayerNamePicker.Choose("Veve69", string.Empty);

        Assert.That(elegido, Is.EqualTo("Veve69"));
    }

    [Test]
    public void SoloLaDelUmbralBajoTieneTexto_DevuelveEsa()
    {
        var elegido = PlayerNamePicker.Choose(string.Empty, "titoparty");

        Assert.That(elegido, Is.EqualTo("titoparty"));
    }

    [Test]
    public void LecturasDistintas_PrefiereLaDelUmbralBajo()
    {
        var elegido = PlayerNamePicker.Choose("ShetfoldI0", "Sbetfold90");

        Assert.That(elegido, Is.EqualTo("Sbetfold90"),
            "El umbral bajo lee mejor los nombres del skin, incluso los brillantes.");
    }

    [Test]
    public void LecturasVacias_DevuelveVacio()
    {
        var elegido = PlayerNamePicker.Choose(string.Empty, "   ");

        Assert.That(elegido, Is.Empty);
    }
}
