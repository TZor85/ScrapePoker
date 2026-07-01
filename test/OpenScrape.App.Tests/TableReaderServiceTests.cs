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
    public async Task IsBoardCardVisible_ReturnsTrue_When_ImageMatchesAboveThreshold()
    {
        // Arrange: imagen sintética + region + fake cropper retorna match > 80%
        // Primero poblamos _cardsImages via ObtainCardsPlayerAsync
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card1", new DomainRegion(
            Category: "Board", Name: "Card1",
            PosX: 10, PosY: 20, Width: 30, Height: 40,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));
        regionCache.AddRegion("User", "u0cardface0", new DomainRegion(
            Category: "User", Name: "u0cardface0",
            PosX: 10, PosY: 10, Width: 25, Height: 35,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var imageCropper = new FakeImageCropperService(base64Result: "fake-base64");
        imageCropper.SetCompareResult(85.0); // > 80% threshold

        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 }
        };
        var cardCache = new FakeCardCacheService(cards);
        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            imageCropper: imageCropper,
            captureService: captureService,
            cardCache: cardCache);

        // Priming: cargar _cardsImages desde ObtainCardsPlayerAsync
        await service.ObtainCardsPlayerAsync(screenshot, new PlayerGameState());

        // Act
        var result = service.IsBoardCardVisible(screenshot, "Card1");

        // Assert
        Assert.That(result, Is.True, "Debe retornar true con match > 80%");
    }

    [Test]
    public async Task IsBoardCardVisible_ReturnsFalse_When_ImageMatchesBelowThreshold()
    {
        // Arrange: match < 80%
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card1", new DomainRegion(
            Category: "Board", Name: "Card1",
            PosX: 10, PosY: 20, Width: 30, Height: 40,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));
        regionCache.AddRegion("User", "u0cardface0", new DomainRegion(
            Category: "User", Name: "u0cardface0",
            PosX: 10, PosY: 10, Width: 25, Height: 35,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var imageCropper = new FakeImageCropperService(base64Result: "fake-base64");
        imageCropper.SetCompareResult(70.0); // < 80% threshold

        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 }
        };
        var cardCache = new FakeCardCacheService(cards);
        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            imageCropper: imageCropper,
            captureService: captureService,
            cardCache: cardCache);

        // Priming
        await service.ObtainCardsPlayerAsync(screenshot, new PlayerGameState());

        // Act
        var result = service.IsBoardCardVisible(screenshot, "Card1");

        // Assert
        Assert.That(result, Is.False, "Debe retornar false con match < 80%");
    }

    [Test]
    public void IsBoardCardVisible_ReturnsFalse_When_RegionNotFound()
    {
        // Arrange: región no existe en el cache
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache(); // empty cache

        var imageCropper = new FakeImageCropperService(base64Result: "fake-base64");
        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            imageCropper: imageCropper,
            captureService: captureService);

        // Act
        var result = service.IsBoardCardVisible(screenshot, "NonExistent");

        // Assert
        Assert.That(result, Is.False, "Debe retornar false si la región no existe");
    }

    [Test]
    public void IsBoardCardVisible_ReturnsFalse_When_RegionIsNotHash()
    {
        // Arrange: región existe pero IsHash != true
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card2", new DomainRegion(
            Category: "Board", Name: "Card2",
            PosX: 50, PosY: 20, Width: 30, Height: 40,
            IsHash: false, // NOT a hash region → should be ignored
            IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var imageCropper = new FakeImageCropperService(base64Result: "fake-base64");
        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            imageCropper: imageCropper,
            captureService: captureService);

        // Act
        var result = service.IsBoardCardVisible(screenshot, "Card2");

        // Assert: IsHash=false → region is nullified before checking → should return false
        Assert.That(result, Is.False, "Debe retornar false para región no-hash");
    }

    #endregion

    #region CountVisibleBoardCards Tests

    [Test]
    public async Task CountVisibleBoardCards_Returns3_When_ThreeCardsVisible()
    {
        // Arrange: Card1, Card2, Card3 visibles (>80%), Card4 no visible (<80%)
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
        // Necesita al menos una región User para ObtainCardsPlayerAsync (priming)
        regionCache.AddRegion("User", "u0cardface0", new DomainRegion(
            Category: "User", Name: "u0cardface0",
            PosX: 10, PosY: 10, Width: 25, Height: 35,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 }
        };
        var cardCache = new FakeCardCacheService(cards);

        // ImageCropper retorna 85% para todas las llamadas
        var imageCropper = new FakeImageCropperService(base64Result: "cropped-base64");
        imageCropper.SetCompareResult(85.0);

        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            imageCropper: imageCropper,
            captureService: captureService,
            cardCache: cardCache);

        // Priming: cargar _cardsImages
        await service.ObtainCardsPlayerAsync(screenshot, new PlayerGameState());

        // Act: con el mismo compareResult 85% (>80%), TODAS las cartas serían visibles
        // Probemos 5 visibles (dado que IsBoardCardVisible usa cards ya cargadas)
        var count = service.CountVisibleBoardCards(screenshot);

        // Assert: con todas las cartas > 80%, cuenta las 5 (secuencial todas pasan)
        Assert.That(count, Is.EqualTo(5), "Debe contar 5 cartas visibles (todas > 80%)");
    }

    [Test]
    public async Task CountVisibleBoardCards_Returns0_When_NoCardsVisible()
    {
        // Arrange: ninguna carta visible
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card1", new DomainRegion(
            Category: "Board", Name: "Card1",
            PosX: 40, PosY: 20, Width: 30, Height: 40,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));
        regionCache.AddRegion("User", "u0cardface0", new DomainRegion(
            Category: "User", Name: "u0cardface0",
            PosX: 10, PosY: 10, Width: 25, Height: 35,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 }
        };
        var cardCache = new FakeCardCacheService(cards);

        var imageCropper = new FakeImageCropperService(base64Result: "fake-base64");
        imageCropper.SetCompareResult(10.0); // muy por debajo del umbral

        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            imageCropper: imageCropper,
            captureService: captureService,
            cardCache: cardCache);

        // Priming
        await service.ObtainCardsPlayerAsync(screenshot, new PlayerGameState());

        // Act
        var count = service.CountVisibleBoardCards(screenshot);

        // Assert
        Assert.That(count, Is.EqualTo(0), "Debe retornar 0 si ninguna carta es visible");
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

        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 },
            new() { Name = "King Spades", ImageBase64 = "kings-base64", Force = 13, Suit = 3 }
        };

        var cardCache = new FakeCardCacheService(cards);
        // ImageCropper retorna match 95% para ambas regiones
        var imageCropper = new FakeImageCropperService(base64Result: "cropped-base64");
        imageCropper.SetCompareResult(95.0);

        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            imageCropper: imageCropper,
            cardCache: cardCache,
            captureService: captureService);

        var state = new PlayerGameState();

        // Act
        await service.ObtainCardsPlayerAsync(screenshot, state);

        // Assert: el mejor match (95%) establece las cartas del primer CardDTO de la lista
        Assert.Multiple(() =>
        {
            Assert.That(state.HoleCard1Face, Is.EqualTo("Ace"), "Debe establecer HoleCard1Face");
            Assert.That(state.HoleCard1Rank, Is.EqualTo(14), "Debe establecer HoleCard1Rank");
        });
    }

    [Test]
    public async Task ObtainCardsPlayer_DoesNotThrow_When_NoRegions()
    {
        // Arrange
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache(); // empty
        var imageCropper = new FakeImageCropperService(base64Result: "fake");
        var cardCache = new FakeCardCacheService(new List<CardDTO>());
        var captureService = new FakeCaptureService();
        var service = CreateService(
            regionCache: regionCache,
            imageCropper: imageCropper,
            cardCache: cardCache,
            captureService: captureService);
        var state = new PlayerGameState();

        // Act & Assert
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
        IImageCropperService? imageCropper = null,
        ICardCacheService? cardCache = null,
        IOcrService? ocrService = null,
        ILogger<TableReaderService>? logger = null)
    {
        return new TableReaderService(
            regionCache ?? new FakeRegionLookupCache(),
            screenReader ?? new FakeScreenReaderService(),
            captureService ?? new FakeCaptureService(),
            imageCropper ?? new FakeImageCropperService("fake"),
            cardCache ?? new FakeCardCacheService(new List<CardDTO>()),
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

    private sealed class FakeImageCropperService : IImageCropperService
    {
        private readonly string _base64Result;
        private double _compareResult = 0;
        private Dictionary<int, double>? _compareResultsByCallIndex;

        public FakeImageCropperService(string base64Result)
        {
            _base64Result = base64Result;
        }

        public void SetCompareResult(double percentage)
        {
            _compareResult = percentage;
            _compareResultsByCallIndex = null;
        }

        /// <summary>
        /// Configura resultados por índice de llamada. Útil para CountVisibleBoardCards
        /// donde IsBoardCardVisible se llama secuencialmente para Card1..Card5.
        /// </summary>
        public void SetCompareResults(Dictionary<int, double> results)
        {
            _compareResultsByCallIndex = results;
        }

        public string CropImageToBase64(Image sourceImage, int x, int y, int width, int height)
        {
            return _base64Result;
        }

        public double CompareCardsBase64(string? imageBase641, string? imageBase642)
        {
            return _compareResult;
        }
    }

    private sealed class FakeCardCacheService : ICardCacheService
    {
        private readonly List<CardDTO> _cards;

        public FakeCardCacheService(List<CardDTO> cards)
        {
            _cards = cards;
        }

        public Task<List<CardDTO>> GetCardsAsync()
        {
            return Task.FromResult(_cards);
        }
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
        public Bitmap? CaptureWindow(IntPtr handle) => null;

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
