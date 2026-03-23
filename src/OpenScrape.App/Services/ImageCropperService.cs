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

    // Distancia Hamming máxima entre dHashes para considerar candidatos a comparación pixel-a-pixel.
    // Imágenes idénticas tienen distancia 0; cartas completamente distintas ~32+.
    // Umbral conservador: si distancia > 15, imposible que sean ≥90% similares.
    private const int DHashMaxDistance = 15;

    // Cache LRU para bytes de imagen decodificados (evita re-decodificar base64 repetidamente)
    private readonly LruCache<string, byte[]> _imageCache = new(MaxImageCacheSize);
    private readonly ConcurrentDictionary<string, WeakReference<Image>> _imageCache2 = new();

    // Cache LRU de dHash: evita recomputar el hash perceptual de la misma imagen base64
    private readonly LruCache<string, ulong> _dHashCache = new(600);

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
            // Fase 1: Pre-filtro dHash — O(1) comparación de hashes perceptuales.
            // Si la distancia Hamming supera el umbral, las imágenes son demasiado distintas
            // para alcanzar el 90% de similitud: descartamos sin comparación pixel-a-pixel.
            ulong hash1 = GetOrComputeDHash(base64Image1);
            ulong hash2 = GetOrComputeDHash(base64Image2);
            int hammingDistance = HammingDistance(hash1, hash2);
            if (hammingDistance > DHashMaxDistance)
                return 0;

            // Fase 2: Comparación pixel-a-pixel solo para candidatos con hash similar
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

    /// <summary>
    /// Calcula o recupera del caché el dHash de 64 bits de una imagen base64.
    /// </summary>
    private ulong GetOrComputeDHash(string base64Image)
    {
        return _dHashCache.GetOrAdd(base64Image, key =>
        {
            var imageBytes = GetOrAddToCache(key);
            return ComputeDHashFromBytes(imageBytes);
        });
    }

    /// <summary>
    /// Calcula el dHash (difference hash) de 64 bits a partir de bytes de imagen.
    /// Algoritmo: redimensionar a 9x8, comparar píxeles horizontalmente adyacentes.
    /// Imágenes visualmente similares producen hashes con baja distancia Hamming.
    /// </summary>
    private static ulong ComputeDHashFromBytes(byte[] imageBytes)
    {
        try
        {
            using var ms = new MemoryStream(imageBytes, writable: false);
            using var original = SKBitmap.Decode(ms);
            if (original == null) return 0;

            // Redimensionar a 9x8 para generar 64 bits de hash (8 filas × 8 comparaciones)
            using var resized = original.Resize(new SKImageInfo(9, 8), SKFilterQuality.Low);
            if (resized == null) return 0;

            ulong hash = 0;
            int bitIndex = 0;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var pixel1 = resized.GetPixel(x, y);
                    var pixel2 = resized.GetPixel(x + 1, y);

                    int gray1 = (pixel1.Red + pixel1.Green + pixel1.Blue) / 3;
                    int gray2 = (pixel2.Red + pixel2.Green + pixel2.Blue) / 3;

                    if (gray1 > gray2)
                        hash |= 1UL << bitIndex;

                    bitIndex++;
                }
            }

            return hash;
        }
        catch
        {
            return 0; // Si falla el hash, se procede a comparación pixel-a-pixel
        }
    }

    /// <summary>
    /// Calcula la distancia Hamming entre dos hashes de 64 bits.
    /// Usa popcount (contar bits en 1) sobre el XOR de ambos hashes.
    /// </summary>
    private static int HammingDistance(ulong hash1, ulong hash2)
    {
        ulong xor = hash1 ^ hash2;
        // Algoritmo de Brian Kernighan para contar bits en 1
        int count = 0;
        while (xor != 0)
        {
            xor &= xor - 1;
            count++;
        }
        return count;
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

        if (bytes1 == null || bytes2 == null)
            return 0;

        // Umbral de similitud mínimo: 90% de totalPixeles deben ser similares
        int umbralMinimo = (int)(totalPixeles * SimilarityThreshold / 100.0);

        // Terminación temprana: verificar cada N píxeles si aún es alcanzable el umbral
        // Si los píxeles restantes + similares acumulados < umbral, imposible alcanzarlo
        const int intervaloVerificacion = 32; // verificar cada 32 píxeles
        int pixelesProcesados = 0;

        // Procesar los bytes de 4 en 4 (ARGB)
        for (int i = 0; i < bytes1.Length; i += 4)
        {
            if (IsPixelSimilar(bytes1, bytes2, i))
                pixelesSimilares++;

            pixelesProcesados++;

            // Terminación temprana: cada 'intervaloVerificacion' píxeles
            if (pixelesProcesados % intervaloVerificacion == 0)
            {
                int pixelesRestantes = totalPixeles - pixelesProcesados;
                // Máximo posible = similares acumulados + todos los restantes
                if (pixelesSimilares + pixelesRestantes < umbralMinimo)
                    return 0; // Imposible alcanzar el umbral
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
