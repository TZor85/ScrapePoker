//using Android.Icu.Number;

using SkiaSharp;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using Tesseract;


namespace OpenScrape.App.Services;

public class OcrService
{
    private readonly string _tessdataPath;
    public event Action<string> OnDebugImageGenerated;

    private const int MaxBitmapCacheSize = 200;
    private const int MaxOcrCacheSize = 500;

    private TesseractEngine _engine;
    private readonly LruCache<string, SKBitmap> _bitmapCache = new(MaxBitmapCacheSize);
    private readonly LruCache<ulong, string> _ocrCache = new(MaxOcrCacheSize);
    private static readonly object _lock = new object();

    private void InitializeEngine()
    {
        if (_engine != null && !_engine.IsDisposed)
        {
            _engine.Dispose();
        }
        _engine = new TesseractEngine(@"./tessdata", "spa", EngineMode.Default);
    }

    public OcrService()
    {
        // Usamos el directorio de la aplicación
        _tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
        _engine = new TesseractEngine(Path.Combine(_tessdataPath, "tessdata"), "eng", EngineMode.Default);

        try
        {
            // Crear directorio tessdata si no existe
            var tessdataDir = Path.Combine(_tessdataPath, "tessdata");
            if (!Directory.Exists(tessdataDir))
            {
                Directory.CreateDirectory(tessdataDir);
            }

            // Copiar archivo traineddata si no existe
            var trainedDataPath = Path.Combine(tessdataDir, "eng.traineddata");
            if (!File.Exists(trainedDataPath))
            {
                using var stream = GetType().Assembly.GetManifestResourceStream("OpenScrape.App.Resources.tessdata.eng.traineddata");
                if (stream == null)
                {
                    throw new Exception("No se pudo encontrar el archivo eng.traineddata en los recursos.");
                }
                using var fileStream = File.Create(trainedDataPath);
                stream.CopyTo(fileStream);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error inicializando OCR: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Limpia el cache de resultados OCR para forzar re-lectura en la siguiente llamada.
    /// Útil cuando se sabe que la imagen ha cambiado (ej: nuevo street, nuevo stack).
    /// </summary>
    public void ClearCache()
    {
        _ocrCache.Clear();
    }

    // Método general para texto normal
    public async Task<string> ExtractTextFromRegionAsync(string imagePath, int x, int y, int width, int height)
    {
        return await ExtractTextFromRegionAsync(imagePath, x, y, width, height, OcrMode.Normal);
    }

    // Método específico para detectar BB (Big Blinds)
    public async Task<string> ExtractBBFromRegionAsync(string imagePath, int x, int y, int width, int height)
    {
        return await ExtractTextFromRegionAsync(imagePath, x, y, width, height, OcrMode.BB);
    }

    private enum OcrMode
    {
        Normal,
        BB
    }

    private async Task<string> ExtractTextFromRegionAsync(string imagePath, int x, int y, int width, int height, OcrMode mode)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // Asegurarse de que el engine está disponible
                    if (_engine == null || _engine.IsDisposed)
                    {
                        InitializeEngine();
                    }

                    using var originalBitmap = SKBitmap.Decode(imagePath);
                    using var croppedBitmap = new SKBitmap(width, height);
                    using var canvas = new SKCanvas(croppedBitmap);

                    var sourceRect = new SKRectI(x, y, x + width, y + height);
                    canvas.DrawBitmap(originalBitmap, sourceRect, new SKRect(0, 0, width, height));

                    // Aplicar efectos según el modo
                    if (mode == OcrMode.BB)
                    {
                        using var paint = new SKPaint();
                        paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                        {
                            2.0f, 0, 0, 0, -0.2f,
                            0, 2.0f, 0, 0, -0.2f,
                            0, 0, 2.0f, 0, -0.2f,
                            0, 0, 0, 1.0f, 0
                        });
                        canvas.DrawBitmap(croppedBitmap, new SKPoint(0, 0), paint);
                    }

                    using var processedMs = new MemoryStream();
                    using var image = SKImage.FromBitmap(croppedBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    data.SaveTo(processedMs);
                    processedMs.Position = 0;

                    // Configurar Tesseract según el modo
                    if (mode == OcrMode.BB)
                    {
                        _engine.SetVariable("tessedit_char_whitelist", "0123456789.BB");
                        _engine.SetVariable("classify_bln_numeric_mode", "1");
                    }
                    else
                    {
                        _engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.-/ ");
                    }

                    using var img = Pix.LoadFromMemory(processedMs.ToArray());
                    using var page = _engine.Process(img);

                    return page.GetText().Trim();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error en OCR: {ex.Message}", ex);
                }
            }
        });
    }

    public OcrResult ExtractTextFromRegionAndDebug(Image sourceImage, int x, int y, int width, int height, double umbral = 0, bool onlyNumber = false)
    {
        lock (_lock)
        {
            try
            {
                // Asegurarse de que el engine está disponible
                if (_engine == null || _engine.IsDisposed)
                {
                    InitializeEngine();
                }

                // Crear el resultado fuera para poder manejarlo en el finally si es necesario
                OcrResult result = null;

                using (var croppedBitmap = GetCroppedBitmap(sourceImage, x, y, width, height))
                {
                    // Compute dHash for cache
                    ulong hash = ComputeDHash(croppedBitmap);
                    if (_ocrCache.TryGet(hash, out var cachedText))
                    {
                        // Cache hit: crear bitmap independiente del stream (clonar para evitar use-after-dispose)
                        using var ms = new MemoryStream();
                        using var skImage = SKImage.FromBitmap(croppedBitmap);
                        using var encoded = skImage.Encode(SKEncodedImageFormat.Png, 100);
                        encoded.SaveTo(ms);
                        ms.Position = 0;
                        using var tempBitmap = new Bitmap(ms);
                        result = new OcrResult
                        {
                            Text = cachedText,
                            Image = new Bitmap(tempBitmap) // Clon independiente del stream
                        };
                        return result;
                    }

                    using (var processedBitmap = ProcessBitmap(croppedBitmap, width, height, umbral))
                    using (var debugMs = new MemoryStream())
                    {
                        // Procesar la imagen
                        using (var debugImage = SKImage.FromBitmap(processedBitmap))
                        {
                            var encoded = debugImage.Encode(SKEncodedImageFormat.Png, 100);
                            encoded.SaveTo(debugMs);
                            encoded.Dispose(); // Asegurar que se libera el encoded
                        }

                        // Configurar Tesseract
                        ConfigureTesseract(onlyNumber);

                        // Convertir a array una sola vez
                        byte[] imageData = debugMs.ToArray();

                        // Invertir colores en la imagen (si es necesario)
                        using (var ms = new MemoryStream(imageData))
                        using (var invertedMs = new MemoryStream())
                        {
                            using (var bitmap = new Bitmap(ms))
                            {
                                Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                                BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                                int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

                                unsafe
                                {
                                    byte* ptr = (byte*)data.Scan0;
                                    int totalBytes = bitmap.Height * data.Stride;

                                    for (int i = 0; i < totalBytes; i += bytesPerPixel)
                                    {
                                        ptr[i] = (byte)(255 - ptr[i]);     // B
                                        ptr[i + 1] = (byte)(255 - ptr[i + 1]); // G
                                        ptr[i + 2] = (byte)(255 - ptr[i + 2]); // R
                                        // Alpha se mantiene si existe
                                    }
                                }

                                bitmap.UnlockBits(data);

                                // Guardar la imagen invertida
                                bitmap.Save(invertedMs, System.Drawing.Imaging.ImageFormat.Png);
                            }

                            // Usar la imagen invertida para OCR
                            imageData = invertedMs.ToArray();
                        }

                        // Procesar OCR con scoring de confianza
                        using (var img = Pix.LoadFromMemory(imageData))
                        using (var page = _engine.Process(img))
                        {
                            var text = ProcessText(page.GetText().Trim());
                            var confidence = page.GetMeanConfidence();

                            // Cache the result — LruCache descarta la entrada menos usada al superar la capacidad
                            _ocrCache.Set(hash, text);

                            // Crear bitmap independiente del stream (clonar para evitar use-after-dispose)
                            using (var ms = new MemoryStream(imageData))
                            using (var tempBitmap = new Bitmap(ms))
                            {
                                result = new OcrResult
                                {
                                    Text = text,
                                    Image = new Bitmap(tempBitmap),
                                    Confidence = confidence
                                };
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _engine?.Dispose(); // Intentar liberar el engine si algo falla
                _engine = null;
                throw new Exception($"Error en OCR: {ex.Message}", ex);
            }
        }
    }

    private SKBitmap GetCroppedBitmap(Image sourceImage, int x, int y, int width, int height)
    {
        var key = $"{sourceImage.GetHashCode()}_{x}_{y}_{width}_{height}";

        if (_bitmapCache.TryGet(key, out var cachedBitmap))
        {
            return cachedBitmap.Copy();
        }

        using var ms = new MemoryStream();
        sourceImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;

        using var originalBitmap = SKBitmap.Decode(ms);
        var croppedBitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(croppedBitmap);

        var sourceRect = new SKRectI(x, y, x + width, y + height);
        canvas.DrawBitmap(originalBitmap, sourceRect, new SKRect(0, 0, width, height));

        // LruCache descarta el bitmap menos reciente cuando supera la capacidad
        _bitmapCache.Set(key, croppedBitmap.Copy());
        return croppedBitmap;
    }

    private SKBitmap ProcessBitmap(SKBitmap croppedBitmap, int width, int height, double porcentaje)
    {
        var processedBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        var thresholdValue = (int)(porcentaje * 255);

        // Acceso directo a memoria de píxeles — evita GetPixel/SetPixel que tienen overhead
        // por llamada. ReadOnlySpan<byte> para lectura, Span<byte> para escritura.
        var srcSpan = croppedBitmap.GetPixelSpan();  // BGRA fuente (read-only)
        var dstSpan = processedBitmap.GetPixelSpan(); // BGRA destino (writable)

        // Cada píxel = 4 bytes: B, G, R, A (formato BGRA8888)
        int totalBytes = width * height * 4;

        for (int i = 0; i < totalBytes; i += 4)
        {
            // Leer B, G, R del pixel fuente
            int b = srcSpan[i];
            int g = srcSpan[i + 1];
            int r = srcSpan[i + 2];

            // Calcular brillo promedio y binarizar
            int brightness = (r + g + b) / 3;
            byte value = brightness > thresholdValue ? (byte)255 : (byte)0;

            // Escribir pixel blanco o negro en destino
            dstSpan[i] = value; // B
            dstSpan[i + 1] = value; // G
            dstSpan[i + 2] = value; // R
            dstSpan[i + 3] = 255;   // A (opaco)
        }

        return processedBitmap;
    }

    private void ConfigureTesseract(bool onlyNumber)
    {
        var charWhiteList = onlyNumber ? "0123456789." : "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.-/";
        _engine.SetVariable("tessedit_char_whitelist", charWhiteList);
        _engine.SetVariable("tessedit_pageseg_mode", "7");
        _engine.SetVariable("classify_bln_numeric_mode", "0");
        _engine.SetVariable("textord_min_linesize", "2.5");
        _engine.SetVariable("textord_debug_block", "0");
        _engine.SetVariable("edges_max_children_per_outline", "40");

    }

    private string ProcessText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        text = text.Replace(".", ",").Replace(" ", ",");

        if (text.Contains(',') && decimal.TryParse(text, out decimal dec))
        {
            return (Math.Truncate(dec * 100) / 100.0m).ToString("F2");
        }

        return text;
    }

    private ulong ComputeDHash(SKBitmap bitmap)
    {
        using var normalized = bitmap.Resize(new SKImageInfo(64, 64), SKFilterQuality.Medium);
        using var hashBitmap = normalized.Resize(new SKImageInfo(9, 8), SKFilterQuality.None);

        ulong hash = 0;
        int bitIndex = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var pixel1 = hashBitmap.GetPixel(x, y);
                var pixel2 = hashBitmap.GetPixel(x + 1, y);
                var gray1 = (pixel1.Red + pixel1.Green + pixel1.Blue) / 3;
                var gray2 = (pixel2.Red + pixel2.Green + pixel2.Blue) / 3;
                if (gray1 > gray2)
                {
                    hash |= (1UL << bitIndex);
                }
                bitIndex++;
            }
        }
        return hash;
    }

    public void ClearBitmapCache()
    {
        foreach (var bitmap in _bitmapCache.Values)
        {
            bitmap.Dispose();
        }
        _bitmapCache.Clear();
    }


}

public class OcrResult : IDisposable
{
    public string? Text { get; set; }
    public Bitmap? Image { get; set; }

    /// <summary>
    /// Confianza media de Tesseract (0.0 a 1.0). -1 si no disponible (cache hit).
    /// </summary>
    public float Confidence { get; set; } = -1;

    /// <summary>
    /// Indica si la confianza del OCR es suficiente para confiar en el resultado.
    /// </summary>
    public bool IsHighConfidence => Confidence < 0 || Confidence >= 0.70f;

    public void Dispose()
    {
        Image?.Dispose();
        Image = null;
    }
}
