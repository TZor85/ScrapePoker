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
        _tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
        _engine = new TesseractEngine(Path.Combine(_tessdataPath, "tessdata"), "eng", EngineMode.Default);

        try
        {
            var tessdataDir = Path.Combine(_tessdataPath, "tessdata");
            if (!Directory.Exists(tessdataDir))
            {
                Directory.CreateDirectory(tessdataDir);
            }

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

    public void ClearCache()
    {
        _ocrCache.Clear();
    }

    public async Task<string> ExtractTextFromRegionAsync(string imagePath, int x, int y, int width, int height)
    {
        return await ExtractTextFromRegionAsync(imagePath, x, y, width, height, OcrMode.Normal);
    }

    public async Task<string> ExtractBBFromRegionAsync(string imagePath, int x, int y, int width, int height)
    {
        return await ExtractTextFromRegionAsync(imagePath, x, y, width, height, OcrMode.BB);
    }

    private enum OcrMode { Normal, BB }

    private async Task<string> ExtractTextFromRegionAsync(string imagePath, int x, int y, int width, int height, OcrMode mode)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    if (_engine == null || _engine.IsDisposed)
                    {
                        InitializeEngine();
                    }

                    using var originalBitmap = SKBitmap.Decode(imagePath);
                    using var croppedBitmap = new SKBitmap(width, height);
                    using var canvas = new SKCanvas(croppedBitmap);

                    var sourceRect = new SKRectI(x, y, x + width, y + height);
                    canvas.DrawBitmap(originalBitmap, sourceRect, new SKRect(0, 0, width, height));

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
                if (_engine == null || _engine.IsDisposed)
                {
                    InitializeEngine();
                }

                OcrResult result = null;

                using (var croppedBitmap = GetCroppedBitmap(sourceImage, x, y, width, height))
                {
                    ulong hash = ComputeDHash(croppedBitmap);
                    if (_ocrCache.TryGet(hash, out var cachedText))
                    {
                        using var msCache = new MemoryStream();
                        using var skImage = SKImage.FromBitmap(croppedBitmap);
                        using var encoded = skImage.Encode(SKEncodedImageFormat.Png, 100);
                        encoded.SaveTo(msCache);
                        msCache.Position = 0;
                        using var tempBitmap = new Bitmap(msCache);
                        result = new OcrResult
                        {
                            Text = cachedText,
                            Image = new Bitmap(tempBitmap),
                            Confidence = -1
                        };
                        return result;
                    }

                    var ocrResults = new List<(string Text, float Confidence, string Config)>();

                    var attempt1 = TryOcrAttempt(croppedBitmap, width, height, umbral, onlyNumber, "default");
                    if (!string.IsNullOrEmpty(attempt1.Text))
                        ocrResults.Add(attempt1);

                    if (umbral > 0)
                    {
                        var attempt2 = TryOcrAttempt(croppedBitmap, width, height, Math.Max(0, umbral - 20), onlyNumber, "lower");
                        if (!string.IsNullOrEmpty(attempt2.Text))
                            ocrResults.Add(attempt2);

                        var attempt3 = TryOcrAttempt(croppedBitmap, width, height, Math.Min(255, umbral + 20), onlyNumber, "higher");
                        if (!string.IsNullOrEmpty(attempt3.Text))
                            ocrResults.Add(attempt3);
                    }

                    var attempt4 = TryOcrAttempt(croppedBitmap, width, height, umbral, onlyNumber, "contrast");
                    if (!string.IsNullOrEmpty(attempt4.Text))
                        ocrResults.Add(attempt4);

                    var bestResult = ocrResults
                        .Where(r => !string.IsNullOrEmpty(r.Text))
                        .OrderByDescending(r => r.Confidence)
                        .ThenByDescending(r => r.Text.Length)
                        .FirstOrDefault();

                    if (string.IsNullOrEmpty(bestResult.Text) && ocrResults.Count > 0)
                    {
                        bestResult = ocrResults.OrderByDescending(r => r.Text.Length).First();
                    }

                    var finalText = string.IsNullOrEmpty(bestResult.Text) ? "" : ProcessText(bestResult.Text);

                    _ocrCache.Set(hash, finalText);

                    using var debugMs = new MemoryStream();
                    using (var debugImage = SKImage.FromBitmap(croppedBitmap))
                    {
                        var encoded = debugImage.Encode(SKEncodedImageFormat.Png, 100);
                        encoded.SaveTo(debugMs);
                    }

                    using var msResult = new MemoryStream(debugMs.ToArray());
                    result = new OcrResult
                    {
                        Text = finalText,
                        Image = new Bitmap(msResult),
                        Confidence = bestResult.Confidence,
                        Attempts = ocrResults.Count
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                _engine?.Dispose();
                _engine = null;
                throw new Exception($"Error en OCR: {ex.Message}", ex);
            }
        }
    }

    private (string Text, float Confidence, string Config) TryOcrAttempt(SKBitmap croppedBitmap, int width, int height, double threshold, bool onlyNumber, string configName)
    {
        try
        {
            using var processedBitmap = ProcessBitmap(croppedBitmap, width, height, threshold);

            if (configName == "contrast")
            {
                using var contrastBitmap = ApplyContrast(processedBitmap, 1.5f);
                return PerformOcr(contrastBitmap, onlyNumber, configName);
            }

            return PerformOcr(processedBitmap, onlyNumber, configName);
        }
        catch
        {
            return ("", 0, configName);
        }
    }

    private SKBitmap ApplyContrast(SKBitmap source, float contrast)
    {
        var result = new SKBitmap(source.Width, source.Height);
        var srcSpan = source.GetPixelSpan();
        var dstSpan = result.GetPixelSpan();

        for (int i = 0; i < srcSpan.Length; i += 4)
        {
            byte applyContrast(byte value) => (byte)Math.Clamp(((value - 128) * contrast) + 128, 0, 255);

            dstSpan[i] = applyContrast(srcSpan[i]);
            dstSpan[i + 1] = applyContrast(srcSpan[i + 1]);
            dstSpan[i + 2] = applyContrast(srcSpan[i + 2]);
            dstSpan[i + 3] = srcSpan[i + 3];
        }

        return result;
    }

    private (string Text, float Confidence, string Config) PerformOcr(SKBitmap bitmap, bool onlyNumber, string configName)
    {
        using var debugMs = new MemoryStream();
        using (var debugImage = SKImage.FromBitmap(bitmap))
        {
            var encoded = debugImage.Encode(SKEncodedImageFormat.Png, 100);
            encoded.SaveTo(debugMs);
        }

        byte[] imageData = InvertBitmap(debugMs.ToArray());

        ConfigureTesseract(onlyNumber);

        using var img = Pix.LoadFromMemory(imageData);
        using var page = _engine.Process(img);

        var text = page.GetText().Trim();
        var confidence = page.GetMeanConfidence();

        return (text, confidence, configName);
    }

    private byte[] InvertBitmap(byte[] imageData)
    {
        using var msInvert = new MemoryStream(imageData);
        using var invertedMs = new MemoryStream();

        using (var bitmap = new Bitmap(msInvert))
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
                    ptr[i] = (byte)(255 - ptr[i]);
                    ptr[i + 1] = (byte)(255 - ptr[i + 1]);
                    ptr[i + 2] = (byte)(255 - ptr[i + 2]);
                }
            }

            bitmap.UnlockBits(data);
            bitmap.Save(invertedMs, System.Drawing.Imaging.ImageFormat.Png);
        }

        return invertedMs.ToArray();
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

        _bitmapCache.Set(key, croppedBitmap.Copy());
        return croppedBitmap;
    }

    private SKBitmap ProcessBitmap(SKBitmap croppedBitmap, int width, int height, double porcentaje)
    {
        var processedBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        var thresholdValue = (int)(porcentaje * 255);

        var srcSpan = croppedBitmap.GetPixelSpan();
        var dstSpan = processedBitmap.GetPixelSpan();

        int totalBytes = width * height * 4;

        for (int i = 0; i < totalBytes; i += 4)
        {
            int b = srcSpan[i];
            int g = srcSpan[i + 1];
            int r = srcSpan[i + 2];

            int brightness = (r + g + b) / 3;
            byte value = brightness > thresholdValue ? (byte)255 : (byte)0;

            dstSpan[i] = value;
            dstSpan[i + 1] = value;
            dstSpan[i + 2] = value;
            dstSpan[i + 3] = 255;
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
    public float Confidence { get; set; } = -1;
    public int Attempts { get; set; } = 1;

    public bool IsHighConfidence => Confidence < 0 || Confidence >= 0.70f;

    public void Dispose()
    {
        Image?.Dispose();
        Image = null;
    }
}
