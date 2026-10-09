using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenScrape.App.Services;

/// <summary>
/// Extrae los píxeles BGRA de un rectángulo de un <see cref="Bitmap"/> a un buffer
/// contiguo (sin PNG ni base64). Devuelve <c>null</c> si el rectángulo no es válido
/// o queda fuera de los límites de la imagen.
/// </summary>
public static class RegionPixelExtractor
{
    public static byte[]? ExtractBgra(Bitmap source, int x, int y, int width, int height)
    {
        if (source == null || width <= 0 || height <= 0)
            return null;
        if (x < 0 || y < 0 || x + width > source.Width || y + height > source.Height)
            return null;

        var rect = new Rectangle(x, y, width, height);
        BitmapData? data = null;
        try
        {
            data = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int rowBytes = width * 4;
            var buffer = new byte[rowBytes * height];
            for (int row = 0; row < height; row++)
            {
                IntPtr src = IntPtr.Add(data.Scan0, row * data.Stride);
                Marshal.Copy(src, buffer, row * rowBytes, rowBytes);
            }
            return buffer;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (data != null)
                source.UnlockBits(data);
        }
    }
}
