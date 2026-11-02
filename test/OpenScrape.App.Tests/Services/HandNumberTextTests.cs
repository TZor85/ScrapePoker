using OpenScrape.App.Services;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// La región del número de mano puede incluir la etiqueta "ID: ". Con el whitelist de
/// dígitos esas letras se transcriben como números ("ID:" → "10", "1B", "13"...) y
/// contaminan el identificador: una mano real 9150780034 se guardó como 13150780034.
/// </summary>
[TestFixture]
public class HandNumberTextTests
{
    [Test]
    public void Limpiar_ConEtiquetaIDAdelante_DevuelveSoloElNumero()
    {
        Assert.That(HandNumberText.Clean("ID: 9150780034"), Is.EqualTo("9150780034"));
    }

    [Test]
    public void Limpiar_CuandoLaEtiquetaSeLeyoComoDigitos_DevuelveSoloElNumero()
    {
        Assert.That(HandNumberText.Clean("1D: 9150780034"), Is.EqualTo("9150780034"));
    }

    [Test]
    public void Limpiar_NumeroLimpio_LoDejaIgual()
    {
        Assert.That(HandNumberText.Clean("9150780034"), Is.EqualTo("9150780034"));
    }

    [Test]
    public void Limpiar_SinTexto_DevuelveVacio()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HandNumberText.Clean(null), Is.Empty);
            Assert.That(HandNumberText.Clean("   "), Is.Empty);
        });
    }

    [Test]
    public void Limpiar_VariosTramosDeDigitos_SeQuedaConElMasLargo()
    {
        Assert.That(HandNumberText.Clean("12 abc 9150780034"), Is.EqualTo("9150780034"));
    }
}
