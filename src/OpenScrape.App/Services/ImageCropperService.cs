using SkiaSharp;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenScrape.App.Services;

public class ImageCropperService
{
    private const int DefaultTolerance = 10;
    private const double SimilarityThreshold = 90.0;
    private const int MaxImageCacheSize = 500;

    // Cache para evitar decodificar la misma imagen múltiples veces
    private readonly ConcurrentDictionary<string, byte[]> _imageCache = new();
    private readonly ConcurrentDictionary<string, WeakReference<Image>> _imageCache2 = new();
    private static readonly object _lock = new object();

    public string CropImageToBase64(Image sourceImage, int x, int y, int width, int height)
    {
        lock (_lock)
        {
            try
            {
                // Convertir System.Drawing.Image a Stream
                using var ms = new MemoryStream();
                sourceImage.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                // Cargar imagen con SkiaSharp
                using var originalBitmap = SKBitmap.Decode(ms);

                // Crear un nuevo bitmap para la región recortada
                using var croppedBitmap = new SKBitmap(width, height);

                // Crear un canvas para dibujar
                using var canvas = new SKCanvas(croppedBitmap);

                // Definir el área a recortar
                var sourceRect = new SKRectI(x, y, x + width, y + height);
                var destRect = new SKRectI(0, 0, width, height);

                // Dibujar la región recortada
                canvas.DrawBitmap(originalBitmap, sourceRect, destRect);

                // Convertir a base64
                using var image = SKImage.FromBitmap(croppedBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var croppedMs = new MemoryStream();
                data.SaveTo(croppedMs);

                return $"{Convert.ToBase64String(croppedMs.ToArray())}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al recortar la imagen: {ex.Message}", ex);
            }
        }
    }

    public Image Base64ToImage(string base64String)
    {
        try
        {
            // Usar el base64String como clave del cache
            if (_imageCache2.TryGetValue(base64String, out var weakRef))
            {
                // Si la imagen existe en el cache y no ha sido recolectada por el GC
                if (weakRef.TryGetTarget(out var cachedImage))
                {
                    return cachedImage;
                }
                // Si la imagen fue recolectada, la removemos del cache
                _imageCache2.TryRemove(base64String, out _);
            }

            // Procesamiento base64 optimizado
            var imageBytes = GetImageBytesFromBase64(base64String);
            var image = CreateImageFromBytes(imageBytes);

            // Almacenar en cache usando WeakReference
            _imageCache2.TryAdd(base64String, new WeakReference<Image>(image));

            return image;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al convertir base64 a imagen: {ex.Message}", ex);
        }
    }

    public void GuardarEnArchivo(string campo1, string campo2)
    {
        try
        {
            // Ruta del archivo en la raíz del proyecto
            string rutaArchivo = Path.Combine(Application.StartupPath, "datos.txt");

            // Crear la línea a escribir
            string linea = $"\n {campo1} \n {campo2}";  // Incluyo la fecha como ejemplo

            // Añadir la línea al archivo (si no existe, lo crea)
            File.AppendAllText(rutaArchivo, linea + Environment.NewLine);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar en el archivo: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public double CompareCardsBase64(string base64Image1, string base64Image2)
    {
        try
        {
            using var bitmap1 = GetLockedBitmap(base64Image1);
            using var bitmap2 = GetLockedBitmap(base64Image2);

            if (!AreCompatibleDimensions(bitmap1, bitmap2))
                return 0;

            return CalculateSimilarity(bitmap1, bitmap2);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al comparar imágenes de cartas: {ex.Message}", ex);
        }
    }

    private FastBitmap GetLockedBitmap(string base64Image)
    {
        var imageBytes = GetOrAddToCache(base64Image);
        using var ms = new MemoryStream(imageBytes);
        using var image = Image.FromStream(ms);
        return new FastBitmap(new Bitmap(image));
    }

    private byte[] GetOrAddToCache(string base64Image)
    {
        if (_imageCache.Count >= MaxImageCacheSize)
        {
            _imageCache.Clear();
        }
        return _imageCache.GetOrAdd(base64Image, key => Convert.FromBase64String(key));
    }

    private bool AreCompatibleDimensions(FastBitmap bmp1, FastBitmap bmp2)
    {
        return bmp1.Width == bmp2.Width && bmp1.Height == bmp2.Height;
    }

    private double CalculateSimilarity(FastBitmap bmp1, FastBitmap bmp2)
    {
        int pixelesSimilares = 0;
        int totalPixeles = bmp1.Width * bmp1.Height;

        var bytes1 = bmp1.GetBytes();
        var bytes2 = bmp2.GetBytes();

        // Procesar los bytes de 4 en 4 (ARGB)
        for (int i = 0; i < bytes1?.Length; i += 4)
        {
            if (IsPixelSimilar(bytes1, bytes2, i))
            {
                pixelesSimilares++;
            }
        }

        double porcentajeSimilitud = (double)pixelesSimilares / totalPixeles * 100;
        return porcentajeSimilitud >= SimilarityThreshold ? porcentajeSimilitud : 0;
    }

    private bool IsPixelSimilar(byte[] pixels1, byte[]? pixels2, int offset)
    {
        if (pixels2 == null)
        {
            return false;
        }

        return Math.Abs(pixels1[offset + 2] - pixels2[offset + 2]) <= DefaultTolerance && // R
               Math.Abs(pixels1[offset + 1] - pixels2[offset + 1]) <= DefaultTolerance && // G
               Math.Abs(pixels1[offset] - pixels2[offset]) <= DefaultTolerance;   // B
    }

    private static byte[] GetImageBytesFromBase64(string base64String)
    {
        // Optimización: Usar Span para evitar crear strings innecesarios
        ReadOnlySpan<char> base64Span = base64String.AsSpan();
        int commaIndex = base64Span.IndexOf(',');

        if (commaIndex >= 0)
        {
            base64Span = base64Span.Slice(commaIndex + 1);
        }

        return Convert.FromBase64String(base64Span.ToString());
    }

    private static Image CreateImageFromBytes(byte[] imageBytes)
    {
        try
        {
            // Crear MemoryStream con capacidad inicial exacta
            using var ms = new MemoryStream(imageBytes, writable: false);
            return Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false);
        }
        catch
        {
            // Si falla, intentar con el método más seguro
            using var ms = new MemoryStream(imageBytes);
            return Image.FromStream(ms);
        }
    }

    // Método para limpiar el cache cuando sea necesario
    public void CleanImageCache()
    {
        foreach (var kvp in _imageCache2.ToList())
        {
            if (!kvp.Value.TryGetTarget(out _))
            {
                _imageCache2.TryRemove(kvp.Key, out _);
            }
        }
    }
}

// Clase auxiliar para acceso rápido a los datos de la imagen
public class FastBitmap : IDisposable
{
    private readonly Bitmap _bitmap;
    private BitmapData? _bitmapData;
    private byte[]? _bytes;
    private bool _disposed;

    public int Width => _bitmap.Width;
    public int Height => _bitmap.Height;

    public FastBitmap(Bitmap bitmap)
    {
        _bitmap = bitmap;
        Lock();
    }

    ~FastBitmap()
    {
        Dispose(false);
    }

    private void Lock()
    {
        var rect = new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);
        _bitmapData = _bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        int bytes = Math.Abs(_bitmapData.Stride) * _bitmap.Height;
        _bytes = new byte[bytes];
        Marshal.Copy(_bitmapData.Scan0, _bytes, 0, bytes);
    }

    public byte[]? GetBytes()
    {
        return _bytes;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            if (_bitmapData != null)
            {
                _bitmap.UnlockBits(_bitmapData);
                _bitmapData = null;
            }
            _bitmap.Dispose();
        }
    }
}
