using System.Drawing;
using System.Windows.Forms;
using OpenScrape.App.Helpers;

namespace OpenScrape.App.Tests;

/// <summary>
/// Cada imagen asignada al visor tiene que liberar la anterior. Sin eso, navegar
/// 153 capturas deja 153 bitmaps vivos y la app se queda sin handles GDI.
/// </summary>
[TestFixture]
public class ImageSwapperTests
{
    [Test]
    public void Set_ReemplazaLaImagenYLiberaLaAnterior()
    {
        using var target = new PictureBox();
        var primera = new Bitmap(2, 2);
        var segunda = new Bitmap(2, 2);

        ImageSwapper.Set(target, primera);
        ImageSwapper.Set(target, segunda);

        Assert.Multiple(() =>
        {
            Assert.That(target.Image, Is.SameAs(segunda));
            Assert.That(() => primera.Width, Throws.ArgumentException,
                "La imagen reemplazada debe quedar liberada.");
            Assert.That(() => segunda.Width, Throws.Nothing);
        });
    }

    [Test]
    public void Set_ConLaMismaImagen_NoLaLibera()
    {
        using var target = new PictureBox();
        using var imagen = new Bitmap(2, 2);

        ImageSwapper.Set(target, imagen);
        ImageSwapper.Set(target, imagen);

        Assert.Multiple(() =>
        {
            Assert.That(target.Image, Is.SameAs(imagen));
            Assert.That(() => imagen.Width, Throws.Nothing);
        });
    }

    [Test]
    public void Set_ConImagenNula_LiberaLaAnteriorYVaciaElVisor()
    {
        using var target = new PictureBox();
        var anterior = new Bitmap(2, 2);

        ImageSwapper.Set(target, anterior);
        ImageSwapper.Set(target, null);

        Assert.Multiple(() =>
        {
            Assert.That(target.Image, Is.Null);
            Assert.That(() => anterior.Width, Throws.ArgumentException);
        });
    }
}
