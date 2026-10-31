using System.Drawing;
using System.Windows.Forms;

namespace OpenScrape.App.Helpers;

/// <summary>
/// Asigna una imagen a un <see cref="PictureBox"/> liberando la anterior. Sin esto,
/// cada captura mostrada deja un bitmap vivo y navegar decenas de imágenes agota
/// los handles GDI del proceso.
/// </summary>
public static class ImageSwapper
{
    /// <summary>
    /// Reemplaza la imagen del visor. Si es la misma instancia no la libera.
    /// </summary>
    public static void Set(PictureBox target, Image? image)
    {
        ArgumentNullException.ThrowIfNull(target);

        var previous = target.Image;

        if (!ReferenceEquals(previous, image))
            previous?.Dispose();

        target.Image = image;
    }
}
