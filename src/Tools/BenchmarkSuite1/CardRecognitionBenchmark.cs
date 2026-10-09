using System.Drawing;
using System.Drawing.Imaging;
using BenchmarkDotNet.Attributes;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;

namespace BenchmarkSuite1;

/// <summary>
/// Compara el pipeline nuevo (ICardRecognizer: LockBits + dHash shortlist) contra el
/// baseline (CropImageToBase64 + CompareCardsBase64 x52) sobre una región 1920x1080.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class CardRecognitionBenchmark
{
    private const int RegionWidth = 60;
    private const int RegionHeight = 80;

    private Bitmap _screenshot = null!;
    private ImageCropperService _cropper = null!;
    private CardRecognitionEngine _engine = null!;
    private List<CardDTO> _cards = null!;
    private string _regionBase64 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _screenshot = new Bitmap(1920, 1080, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(_screenshot))
            g.Clear(Color.FromArgb(30, 120, 60));

        _cards = new List<CardDTO>(52);
        for (int i = 0; i < 52; i++)
        {
            using var cardBmp = CreatePatternBitmap(RegionWidth, RegionHeight, i);
            _cards.Add(new CardDTO
            {
                Name = $"Card{i}",
                ImageBase64 = ToBase64(cardBmp),
                Force = (i % 13) + 2,
                Suit = i / 13
            });
            if (i == 0)
            {
                using var g = Graphics.FromImage(_screenshot);
                g.DrawImage(cardBmp, 100, 100);
            }
        }

        var index = new CardTemplateIndex(new FakeCardCacheService(_cards));
        index.InitializeAsync().GetAwaiter().GetResult();
        _engine = new CardRecognitionEngine(index);

        _cropper = new ImageCropperService();
        using var regionBmp = new Bitmap(RegionWidth, RegionHeight, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(regionBmp))
            g.DrawImage(_screenshot, new Rectangle(0, 0, RegionWidth, RegionHeight), new Rectangle(100, 100, RegionWidth, RegionHeight), GraphicsUnit.Pixel);
        _regionBase64 = ToBase64(regionBmp);
    }

    [Benchmark(Baseline = true)]
    public double CompareBase64All52()
    {
        double best = 0;
        foreach (var card in _cards)
        {
            if (string.IsNullOrEmpty(card.ImageBase64))
                continue;
            var score = _cropper.CompareCardsBase64(card.ImageBase64, _regionBase64);
            if (score > best)
                best = score;
        }
        return best;
    }

    [Benchmark]
    public double RecognizeWithEngine()
    {
        var match = _engine.Recognize(_screenshot, 100, 100, RegionWidth, RegionHeight);
        return match.Score;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _screenshot.Dispose();
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
}
