using System.Drawing;
using System.Drawing.Imaging;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;
using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Tests;

[TestFixture]
public class CardRecognitionEngineTests
{
    [Test]
    public void PixelComparer_IdenticalBuffers_Returns100()
    {
        var buffer = new byte[4 * 4 * 4]; // 4x4
        for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)(i % 256);

        var score = PixelComparer.CalculateSimilarity(buffer, buffer, 4, 4);

        Assert.That(score, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public void PixelComparer_CompletelyDifferent_Returns0()
    {
        var a = new byte[4 * 4 * 4];
        var b = new byte[4 * 4 * 4];
        for (int i = 0; i < a.Length; i += 4)
        {
            a[i] = 0; a[i + 1] = 0; a[i + 2] = 0;
            b[i] = 255; b[i + 1] = 255; b[i + 2] = 255;
        }

        var score = PixelComparer.CalculateSimilarity(a, b, 4, 4);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void PixelComparer_HammingDistance_CountsDifferingBits()
    {
        Assert.That(PixelComparer.HammingDistance(0UL, 0UL), Is.EqualTo(0));
        Assert.That(PixelComparer.HammingDistance(0b1011UL, 0b1001UL), Is.EqualTo(1));
        Assert.That(PixelComparer.HammingDistance(ulong.MaxValue, 0UL), Is.EqualTo(64));
    }

    [Test]
    public void PixelComparer_EarlyTermination_Returns0_When_ManyDissimilarFirst()
    {
        // 100 pixels: first 50 completely different, last 50 identical.
        // At the 32-pixel checkpoint similarPixels=0 and remaining=68 < 90 → early exit.
        var a = new byte[100 * 4];
        var b = new byte[100 * 4];
        for (int p = 0; p < 100; p++)
        {
            int o = p * 4;
            if (p < 50)
            {
                a[o] = 0; a[o + 1] = 0; a[o + 2] = 0;
                b[o] = 255; b[o + 1] = 255; b[o + 2] = 255;
            }
            else
            {
                a[o] = 10; a[o + 1] = 20; a[o + 2] = 30;
                b[o] = 10; b[o + 1] = 20; b[o + 2] = 30;
            }
        }

        var score = PixelComparer.CalculateSimilarity(a, b, 10, 10);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void PixelComparer_At89Percent_Returns0_At91Percent_Returns91()
    {
        // 89 similar + 11 different → 89 < 90 → 0
        var below = BuildMixed(10, 10, similarPixels: 89);
        Assert.That(PixelComparer.CalculateSimilarity(below.Item1, below.Item2, 10, 10), Is.EqualTo(0.0));

        // 91 similar + 9 different → 91 >= 90 → 91
        var above = BuildMixed(10, 10, similarPixels: 91);
        Assert.That(PixelComparer.CalculateSimilarity(above.Item1, above.Item2, 10, 10), Is.EqualTo(91.0).Within(0.001));
    }

    [Test]
    public void PixelComparer_BufferShorterThanDeclared_Returns0()
    {
        Assert.That(PixelComparer.CalculateSimilarity(new byte[4], new byte[4], 100, 100), Is.EqualTo(0.0));
    }

    [Test]
    public void DHashCalculator_IdenticalBuffers_ReturnsSameHash()
    {
        var bgra = CreatePatternBgra(30, 40, seed: 1);

        var h1 = DHashCalculator.Compute(bgra, 30, 40);
        var h2 = DHashCalculator.Compute(bgra, 30, 40);

        Assert.That(h1, Is.EqualTo(h2));
    }

    [Test]
    public void DHashCalculator_DifferentPatterns_ExceedShortlistThreshold()
    {
        var ba = CreatePatternBgra(30, 40, seed: 1);
        var bb = CreatePatternBgra(30, 40, seed: 99);

        var distance = PixelComparer.HammingDistance(
            DHashCalculator.Compute(ba, 30, 40),
            DHashCalculator.Compute(bb, 30, 40));

        Assert.That(distance, Is.GreaterThan(15),
            "Patrones claramente distintos deben superar el umbral de shortlist (15)");
    }

    [Test]
    public void DHashCalculator_NearIdentical_TinyChange_StaysUnderThreshold()
    {
        var original = CreatePatternBgra(30, 40, seed: 5);
        var modified = (byte[])original.Clone();
        // cambia un solo píxel de forma leve (dentro del rango perceptual)
        modified[0] = (byte)Math.Min(255, modified[0] + 5);

        var distance = PixelComparer.HammingDistance(
            DHashCalculator.Compute(original, 30, 40),
            DHashCalculator.Compute(modified, 30, 40));

        Assert.That(distance, Is.LessThanOrEqualTo(15));
    }

    [Test]
    public void DHashCalculator_InvalidInputs_ReturnsZero()
    {
        Assert.That(DHashCalculator.Compute(new byte[16], 0, 10), Is.EqualTo(0UL));
        Assert.That(DHashCalculator.Compute(new byte[16], 10, -1), Is.EqualTo(0UL));
        Assert.That(DHashCalculator.Compute(new byte[4], 10, 10), Is.EqualTo(0UL));
    }

    [Test]
    public void RegionPixelExtractor_ExtractsRequestedRectangle()
    {
        using var bmp = new Bitmap(50, 50, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
            g.Clear(Color.Green);
        using (var g = Graphics.FromImage(bmp))
            g.FillRectangle(Brushes.Red, new Rectangle(10, 10, 20, 20));

        var buffer = RegionPixelExtractor.ExtractBgra(bmp, 10, 10, 20, 20);

        Assert.That(buffer, Is.Not.Null);
        Assert.That(buffer!.Length, Is.EqualTo(20 * 20 * 4));
        // Primer píxel de la región debe ser rojo (B=0, G=0, R=255)
        Assert.That(buffer[2], Is.EqualTo(255)); // R
        Assert.That(buffer[1], Is.EqualTo(0));   // G
        Assert.That(buffer[0], Is.EqualTo(0));   // B
        // Último píxel de la región también debe ser rojo (valida el avance por Stride)
        int last = (19 * 20 + 19) * 4;
        Assert.That(buffer[last + 2], Is.EqualTo(255)); // R
        Assert.That(buffer[last + 1], Is.EqualTo(0));   // G
        Assert.That(buffer[last], Is.EqualTo(0));       // B
    }

    [Test]
    public void RegionPixelExtractor_OutOfBounds_ReturnsNull()
    {
        using var bmp = new Bitmap(50, 50, PixelFormat.Format32bppArgb);

        Assert.That(RegionPixelExtractor.ExtractBgra(bmp, 40, 40, 20, 20), Is.Null);
        Assert.That(RegionPixelExtractor.ExtractBgra(bmp, 0, 0, 0, 10), Is.Null);
        Assert.That(RegionPixelExtractor.ExtractBgra(bmp, 0, 0, 10, -1), Is.Null);
    }

    [Test]
    public async Task CardTemplateIndex_Initialize_BuildsTemplatesFromCache()
    {
        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 },
            new() { Name = "King Spades", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 2)), Force = 13, Suit = 3 },
            new() { Name = "NoImage", ImageBase64 = null, Force = 0, Suit = 0 }
        };
        var index = new CardTemplateIndex(new FakeCardCacheService(cards));

        await index.InitializeAsync();

        Assert.That(index.IsInitialized, Is.True);
        Assert.That(index.Templates.Count, Is.EqualTo(2), "Las cartas sin ImageBase64 se omiten");
        Assert.That(index.Templates[0].Width, Is.EqualTo(30));
        Assert.That(index.Templates[0].Height, Is.EqualTo(40));
    }

    [Test]
    public async Task CardTemplateIndex_Initialize_IsIdempotent()
    {
        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 }
        };
        var index = new CardTemplateIndex(new FakeCardCacheService(cards));

        await index.InitializeAsync();
        await index.InitializeAsync();

        Assert.That(index.Templates.Count, Is.EqualTo(1));
    }

    [Test]
    public void CardTemplateIndex_Templates_ThrowsBeforeInitialize()
    {
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO>()));

        Assert.That(() => _ = index.Templates, Throws.InvalidOperationException);
    }

    [Test]
    public async Task CardTemplateIndex_Initialize_ConcurrentCalls_BuildOnce()
    {
        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 }
        };
        var index = new CardTemplateIndex(new FakeCardCacheService(cards));

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => index.InitializeAsync()));

        Assert.That(index.IsInitialized, Is.True);
        Assert.That(index.Templates.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task CardTemplateIndex_Initialize_SkipsMalformedBase64()
    {
        var cards = new List<CardDTO>
        {
            new() { Name = "Broken", ImageBase64 = "not-base64!!", Force = 0, Suit = 0 },
            new() { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 }
        };
        var index = new CardTemplateIndex(new FakeCardCacheService(cards));

        await index.InitializeAsync();

        Assert.That(index.Templates.Count, Is.EqualTo(1));
        Assert.That(index.Templates[0].Card.Name, Is.EqualTo("Ace Hearts"));
    }

    [Test]
    public async Task Recognize_IdenticalRegion_ReturnsScore100()
    {
        var cardBmp = CreatePatternBitmap(30, 40, seed: 1);
        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = ToBase64(cardBmp), Force = 14, Suit = 1 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card }));
        await index.InitializeAsync();
        var engine = new CardRecognitionEngine(index);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.DrawImage(cardBmp, 20, 30);

        var match = engine.Recognize(screenshot, 20, 30, 30, 40);

        Assert.That(match.Card, Is.Not.Null);
        Assert.That(match.Card!.Name, Is.EqualTo("Ace Hearts"));
        Assert.That(match.Score, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public async Task Recognize_DimensionMismatch_ReturnsNullCard()
    {
        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card }));
        await index.InitializeAsync();
        var engine = new CardRecognitionEngine(index);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.Clear(Color.Green);

        var match = engine.Recognize(screenshot, 0, 0, 10, 10);

        Assert.That(match.Card, Is.Null);
        Assert.That(match.Score, Is.EqualTo(0.0));
    }

    [Test]
    public async Task IsVisible_ReturnsTrue_When_ScoreAboveMin()
    {
        var cardBmp = CreatePatternBitmap(30, 40, seed: 1);
        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = ToBase64(cardBmp), Force = 14, Suit = 1 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card }));
        await index.InitializeAsync();
        var engine = new CardRecognitionEngine(index);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.DrawImage(cardBmp, 20, 30);

        Assert.That(engine.IsVisible(screenshot, 20, 30, 30, 40, 80.0), Is.True);
    }

    [Test]
    public async Task Recognize_RecordsTelemetry_OcrCards()
    {
        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card }));
        await index.InitializeAsync();
        var metrics = new FakeMetricsCollector();
        var engine = new CardRecognitionEngine(index, metrics);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.DrawImage(CreatePatternBitmap(30, 40, 1), 20, 30);

        engine.Recognize(screenshot, 20, 30, 30, 40);

        Assert.That(metrics.MeasuredCategories, Contains.Item(TelemetryCategories.OcrCards));
    }

    [Test]
    public void PixelComparer_MatchesImageCropperService_ForIdenticalImages()
    {
        var bmp = CreatePatternBitmap(30, 40, seed: 7);
        var base64 = ToBase64(bmp);
        var cropper = new ImageCropperService();

        var cropperScore = cropper.CompareCardsBase64(base64, base64);

        var bgra = RegionPixelExtractor.ExtractBgra(bmp, 0, 0, 30, 40)!;
        var comparerScore = PixelComparer.CalculateSimilarity(bgra, bgra, 30, 40);

        Assert.That(cropperScore, Is.EqualTo(100.0).Within(0.001));
        Assert.That(comparerScore, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public void PixelComparer_MatchesImageCropperService_ForDifferentImages()
    {
        var bmpA = CreatePatternBitmap(30, 40, seed: 1);
        var bmpB = CreatePatternBitmap(30, 40, seed: 200);
        var cropper = new ImageCropperService();

        var cropperScore = cropper.CompareCardsBase64(ToBase64(bmpA), ToBase64(bmpB));

        var bgraA = RegionPixelExtractor.ExtractBgra(bmpA, 0, 0, 30, 40)!;
        var bgraB = RegionPixelExtractor.ExtractBgra(bmpB, 0, 0, 30, 40)!;
        var comparerScore = PixelComparer.CalculateSimilarity(bgraA, bgraB, 30, 40);

        Assert.That(cropperScore, Is.EqualTo(0.0));
        Assert.That(comparerScore, Is.EqualTo(0.0));
    }

    [Test]
    public async Task Recognize_AmongMultipleTemplates_ReturnsHighestScoringCard()
    {
        var card1 = new CardDTO { Name = "Card1", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, seed: 1)), Force = 14, Suit = 1 };
        var card2 = new CardDTO { Name = "Card2", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, seed: 2)), Force = 2, Suit = 2 };
        var card3 = new CardDTO { Name = "Card3", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, seed: 3)), Force = 3, Suit = 3 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card1, card2, card3 }));
        await index.InitializeAsync();
        var engine = new CardRecognitionEngine(index);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.DrawImage(CreatePatternBitmap(30, 40, seed: 2), 10, 10);

        var match = engine.Recognize(screenshot, 10, 10, 30, 40);

        Assert.That(match.Card, Is.Not.Null);
        Assert.That(match.Card!.Name, Is.EqualTo("Card2"));
        Assert.That(match.Score, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public async Task IsVisible_ReturnsFalse_When_RegionDoesNotMatchAnyCard()
    {
        var card = new CardDTO { Name = "Card1", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, seed: 1)), Force = 14, Suit = 1 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card }));
        await index.InitializeAsync();
        var engine = new CardRecognitionEngine(index);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.Clear(Color.Green);

        Assert.That(engine.IsVisible(screenshot, 10, 10, 30, 40, 80.0), Is.False);
    }

    private static (byte[] A, byte[] B) BuildMixed(int width, int height, int similarPixels)
    {
        int total = width * height;
        var a = new byte[total * 4];
        var b = new byte[total * 4];
        for (int p = 0; p < total; p++)
        {
            int o = p * 4;
            if (p < similarPixels)
            {
                a[o] = 10; a[o + 1] = 20; a[o + 2] = 30;
                b[o] = 10; b[o + 1] = 20; b[o + 2] = 30;
            }
            else
            {
                a[o] = 0; a[o + 1] = 0; a[o + 2] = 0;
                b[o] = 255; b[o + 1] = 255; b[o + 2] = 255;
            }
        }
        return (a, b);
    }

    private static byte[] CreatePatternBgra(int width, int height, int seed)
    {
        var buffer = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int o = (y * width + x) * 4;
                byte v = (byte)((x * 7 + y * 13 + seed * 53) % 256);
                buffer[o] = (byte)((v + seed) % 256); // B
                buffer[o + 1] = (byte)(255 - v);       // G
                buffer[o + 2] = v;                      // R
                buffer[o + 3] = 255;                    // A
            }
        }
        return buffer;
    }

    private static Bitmap CreatePatternBitmap(int width, int height, int seed)
    {
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte v = (byte)((x * 7 + y * 13 + seed * 53) % 256);
                bmp.SetPixel(x, y, Color.FromArgb(255, v, (byte)(255 - v), (byte)((v + seed) % 256)));
            }
        }
        return bmp;
    }

    private static string ToBase64(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }

    private sealed class FakeCardCacheService : ICardCacheService
    {
        private readonly List<CardDTO> _cards;
        public FakeCardCacheService(List<CardDTO> cards) => _cards = cards;
        public Task<List<CardDTO>> GetCardsAsync() => Task.FromResult(_cards);
    }

    private sealed class FakeMetricsCollector : IMetricsCollector
    {
        public List<string> MeasuredCategories { get; } = new();

        public ScopedMeasurement Measure(string category)
        {
            MeasuredCategories.Add(category);
            return default;
        }

        public void Record(string category, TimeSpan elapsed) { }
        public ScopedMeasurement MeasureSessionOnly(string category)
        {
            MeasuredCategories.Add(category);
            return default;
        }
        public void RecordSessionOnly(string category, TimeSpan elapsed) { }
        public void StartHand(string handId) { }
        public OpenScrape.Domain.ValueObjects.TelemetryAggregate? EndHand() => null;
        public MetricsSnapshot SnapshotSession() =>
            new(null,
                new Dictionary<string, OpenScrape.Domain.ValueObjects.CategoryStats>(),
                new Dictionary<string, OpenScrape.Domain.ValueObjects.CategoryStats>());
        public void ResetSession() { }
    }
}
