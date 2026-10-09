using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del TableReaderService extraído de FrmMain.
/// Cubre los escenarios de RF3: detección de nueva mano, lectura de cartas,
/// board cards visibles/no visibles, y lectura de apuestas/stacks/pot.
/// </summary>
[TestFixture]
public class TableReaderServiceTests
{
    #region IsBoardCardVisible Tests

    [Test]
    public void IsBoardCardVisible_ReturnsTrue_When_ImageMatchesAboveThreshold()
    {
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card1", new DomainRegion(
            Category: "Board", Name: "Card1",
            PosX: 10, PosY: 20, Width: 30, Height: 40,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.SetResult(card, 85.0);

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var result = service.IsBoardCardVisible(screenshot, "Card1");

        Assert.That(result, Is.True, "Debe retornar true con score > 80");
    }

    [Test]
    public void IsBoardCardVisible_ReturnsFalse_When_ImageMatchesBelowThreshold()
    {
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card1", new DomainRegion(
            Category: "Board", Name: "Card1",
            PosX: 10, PosY: 20, Width: 30, Height: 40,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.SetResult(card, 70.0);

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var result = service.IsBoardCardVisible(screenshot, "Card1");

        Assert.That(result, Is.False, "Debe retornar false con score < 80");
    }

    [Test]
    public void IsBoardCardVisible_ReturnsFalse_When_ScoreExactlyEighty()
    {
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card1", new DomainRegion(
            Category: "Board", Name: "Card1",
            PosX: 10, PosY: 20, Width: 30, Height: 40,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.SetResult(card, 80.0);

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var result = service.IsBoardCardVisible(screenshot, "Card1");

        Assert.That(result, Is.False, "80.0 no es > 80.0, debe ser false");
    }

    [Test]
    public void IsBoardCardVisible_ReturnsFalse_When_RegionNotFound()
    {
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        var service = CreateService(regionCache: regionCache, captureService: new FakeCaptureService());

        var result = service.IsBoardCardVisible(screenshot, "NonExistent");

        Assert.That(result, Is.False, "Debe retornar false si la región no existe");
    }

    [Test]
    public void IsBoardCardVisible_ReturnsFalse_When_RegionIsNotHash()
    {
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card2", new DomainRegion(
            Category: "Board", Name: "Card2",
            PosX: 50, PosY: 20, Width: 30, Height: 40,
            IsHash: false, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var service = CreateService(regionCache: regionCache, captureService: new FakeCaptureService());

        var result = service.IsBoardCardVisible(screenshot, "Card2");

        Assert.That(result, Is.False, "Debe retornar false para región no-hash");
    }

    #endregion

    #region CountVisibleBoardCards Tests

    [Test]
    public void CountVisibleBoardCards_Returns5_When_AllVisible()
    {
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        for (int i = 1; i <= 5; i++)
        {
            regionCache.AddRegion("Board", $"Card{i}", new DomainRegion(
                Category: "Board", Name: $"Card{i}",
                PosX: i * 40, PosY: 20, Width: 30, Height: 40,
                IsHash: true, IsColor: null, IsBoard: null,
                Color: null, IsOnlyNumber: null,
                InactiveUmbral: null, Umbral: null));
        }

        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.SetResult(card, 85.0);

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var count = service.CountVisibleBoardCards(screenshot);

        Assert.That(count, Is.EqualTo(5), "Debe contar 5 cartas visibles (score > 80)");
    }

    [Test]
    public void CountVisibleBoardCards_Returns0_When_NoCardsVisible()
    {
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card1", new DomainRegion(
            Category: "Board", Name: "Card1",
            PosX: 40, PosY: 20, Width: 30, Height: 40,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.SetResult(card, 10.0);

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var count = service.CountVisibleBoardCards(screenshot);

        Assert.That(count, Is.EqualTo(0), "Debe retornar 0 si ninguna carta es visible");
    }

    [Test]
    public void CountVisibleBoardCards_Returns3_When_FourthCardNotVisible()
    {
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        for (int i = 1; i <= 5; i++)
        {
            regionCache.AddRegion("Board", $"Card{i}", new DomainRegion(
                Category: "Board", Name: $"Card{i}",
                PosX: i * 40, PosY: 20, Width: 30, Height: 40,
                IsHash: true, IsColor: null, IsBoard: null,
                Color: null, IsOnlyNumber: null,
                InactiveUmbral: null, Umbral: null));
        }

        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.Enqueue(new CardMatch(card, 90.0));
        recognizer.Enqueue(new CardMatch(card, 90.0));
        recognizer.Enqueue(new CardMatch(card, 90.0));
        recognizer.Enqueue(new CardMatch(null, 0));

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var count = service.CountVisibleBoardCards(screenshot);

        Assert.That(count, Is.EqualTo(3), "Debe cortar en la primera carta no visible");
    }

    #endregion

    #region SetPotValue Tests

    [Test]
    public void SetPotValue_Updates_PotSize_From_OcrReading_With_FourDigits()
    {
        // Arrange: 4 dígitos sin punto decimal (ej: "1525" → 15.25)
        // Este es el patrón típico del OCR en este proyecto
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Table", "pot", new DomainRegion(
            Category: "Table", Name: "pot",
            PosX: 150, PosY: 10, Width: 80, Height: 20,
            IsHash: null, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var screenReader = new FakeScreenReaderService();
        screenReader.SetTextResult("1525"); // 4 dígitos sin punto: se interpreta como 15.25

        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            screenReader: screenReader,
            captureService: captureService);

        var state = new PlayerGameState();

        // Act
        service.SetPotValue(screenshot, state);

        // Assert: "1525" → substring(0,2) + "," + substring(2) → "15,25" → 15.25
        Assert.That(state.PotSize, Is.EqualTo(15.25m), "Debe parsear número de 4 dígitos insertando coma decimal");
    }

    [Test]
    public void SetPotValue_DoesNotThrow_When_RegionNotFound()
    {
        // Arrange: sin región "pot"
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache(); // empty
        var screenReader = new FakeScreenReaderService();
        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            screenReader: screenReader,
            captureService: captureService);
        var state = new PlayerGameState();

        // Act & Assert
        Assert.DoesNotThrow(() => service.SetPotValue(screenshot, state));
    }

    #endregion

    #region ObtainCardsPlayerAsync Tests

    [Test]
    public async Task ObtainCardsPlayer_Populates_HoleCards()
    {
        // Arrange
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("User", "u0cardface0", new DomainRegion(
            Category: "User", Name: "u0cardface0",
            PosX: 10, PosY: 10, Width: 25, Height: 35,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));
        regionCache.AddRegion("User", "u0cardface1", new DomainRegion(
            Category: "User", Name: "u0cardface1",
            PosX: 40, PosY: 10, Width: 25, Height: 35,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var ace = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var king = new CardDTO { Name = "King Spades", ImageBase64 = "kings-base64", Force = 13, Suit = 3 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.Enqueue(new CardMatch(ace, 95.0));
        recognizer.Enqueue(new CardMatch(king, 95.0));

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var state = new PlayerGameState();

        await service.ObtainCardsPlayerAsync(screenshot, state);

        Assert.Multiple(() =>
        {
            Assert.That(state.HoleCard1Face, Is.EqualTo("Ace"), "Debe establecer HoleCard1Face");
            Assert.That(state.HoleCard1Rank, Is.EqualTo(14), "Debe establecer HoleCard1Rank");
            Assert.That(state.HoleCard2Face, Is.EqualTo("King"), "Debe establecer HoleCard2Face");
        });
    }

    [Test]
    public void ObtainCardsPlayer_DoesNotThrow_When_NoRegions()
    {
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        var service = CreateService(regionCache: regionCache, captureService: new FakeCaptureService());
        var state = new PlayerGameState();

        Assert.DoesNotThrowAsync(() => service.ObtainCardsPlayerAsync(screenshot, state));
    }

    #endregion

    #region Helpers: Fake Classes

    /// <summary>
    /// Crea una instancia de TableReaderService con dependencias fake.
    /// </summary>
    private static TableReaderService CreateService(
        IRegionLookupCache? regionCache = null,
        IScreenReaderService? screenReader = null,
        ICaptureService? captureService = null,
        ICardRecognizer? cardRecognizer = null,
        IOcrService? ocrService = null,
        ILogger<TableReaderService>? logger = null)
    {
        return new TableReaderService(
            regionCache ?? new FakeRegionLookupCache(),
            screenReader ?? new FakeScreenReaderService(),
            captureService ?? new FakeCaptureService(),
            cardRecognizer ?? new FakeCardRecognizerService(),
            ocrService ?? new FakeOcrService(),
            logger ?? new FakeLogger<TableReaderService>());
    }

    /// <summary>
    /// Crea un bitmap sintético de color sólido.
    /// </summary>
    private static Bitmap CreateSyntheticBitmap(int width, int height, Color color)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(color);
        return bitmap;
    }

    private sealed class FakeRegionLookupCache : IRegionLookupCache
    {
        private readonly Dictionary<string, Dictionary<string, DomainRegion>> _regionsByMap = new();

        public void AddRegion(string mapId, string regionName, DomainRegion region)
        {
            if (!_regionsByMap.ContainsKey(mapId))
                _regionsByMap[mapId] = new Dictionary<string, DomainRegion>();
            _regionsByMap[mapId][regionName] = region;
        }

        public DomainRegion? GetRegion(string mapId, string regionName)
        {
            if (_regionsByMap.TryGetValue(mapId, out var nameDict) &&
                nameDict.TryGetValue(regionName, out var region))
            {
                return region;
            }
            return null;
        }

        public List<DomainRegion>? GetRegions(string mapId)
        {
            if (_regionsByMap.TryGetValue(mapId, out var nameDict))
                return nameDict.Values.ToList();
            return null;
        }
    }

    private sealed class FakeCardRecognizerService : ICardRecognizer
    {
        private CardMatch _result = new(null, 0);
        private readonly Queue<CardMatch> _queued = new();

        public void SetResult(CardDTO? card, double score) => _result = new CardMatch(card, score);

        public void Enqueue(CardMatch match) => _queued.Enqueue(match);

        public CardMatch Recognize(Bitmap screenshot, int x, int y, int width, int height)
            => _queued.Count > 0 ? _queued.Dequeue() : _result;

        public IReadOnlyList<CardMatch> RankCandidates(Bitmap screenshot, int x, int y, int width, int height)
        {
            var match = Recognize(screenshot, x, y, width, height);
            return match.Card != null ? new List<CardMatch> { match } : new List<CardMatch>();
        }

        public bool IsVisible(Bitmap screenshot, int x, int y, int width, int height, double minScore = 80.0)
            => Recognize(screenshot, x, y, width, height).Score > minScore;
    }

    private sealed class FakeOcrService : IOcrService
    {
        public void ClearCache() { }
    }

    private sealed class FakeScreenReaderService : IScreenReaderService
    {
        private string _textResult = string.Empty;
        private decimal _betValue;
        private decimal _stackValue;

        public void SetTextResult(string text) => _textResult = text;

        public void SetBetValue(decimal value) => _betValue = value;

        public void SetStackValue(decimal value) => _stackValue = value;

        public string ReadPlayerName(Image screenshot, int x, int y, int w, int h, double umbral, double inactiveUmbral)
            => _textResult;

        public decimal ReadBetValue(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber, int? playerNum = null)
            => _betValue;

        public decimal ReadStackValue(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
            => _stackValue;

        public string ReadHandNumber(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
            => _textResult;

        public string ReadText(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
            => _textResult;

        public string ReadTextWithMultipleThresholds(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
            => _textResult;

        public decimal NormalizeBetValue(decimal rawValue, decimal potSize = 0)
            => rawValue;

        public decimal NormalizeStackValue(decimal rawValue)
            => rawValue;
    }

    private sealed class FakeCaptureService : ICaptureService
    {
        public IntPtr WindowHandle { get; set; }
        private Bitmap? _captureResult;

        public void SetCaptureResult(Bitmap? bitmap) => _captureResult = bitmap;

        public Bitmap? CaptureWindow(IntPtr handle) => _captureResult;

        public Bitmap? CaptureWindow() => _captureResult;

        public DomainRegion GetScaledRegion(DomainRegion region)
        {
            // Retorna la misma región sin escalar (coordenadas idénticas)
            return new DomainRegion(
                region.Category, region.Name,
                region.PosX, region.PosY, region.Width, region.Height,
                region.IsHash, region.IsColor, region.IsBoard,
                region.Color, region.IsOnlyNumber,
                region.InactiveUmbral, region.Umbral);
        }

        public void SaveReferenceDimensions(int width, int height) { }

        public string? GetJsonValue(string key, string section) => null;
    }

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    #endregion
}
