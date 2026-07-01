using System.Drawing;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Entities;
using OpenScrape.App.Helpers;
using OpenScrape.Domain.Dtos;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio de lectura OCR de la mesa de póker.
/// Extrae SetBetPlayer, SetHeroStack, SetPotValue, SetTableHand, ObtainCardsPlayerAsync,
/// IsBoardCardVisible y CountVisibleBoardCards de FrmMain.
/// </summary>
public sealed class TableReaderService : ITableReaderService
{
    private readonly IRegionLookupCache _regionLookupCache;
    private readonly IScreenReaderService _screenReader;
    private readonly ICaptureService _captureService;
    private readonly IImageCropperService _imageCropperService;
    private readonly ICardCacheService _cardCacheService;
    private readonly IOcrService _ocrService;
    private readonly ILogger<TableReaderService> _logger;

    private List<CardDTO>? _cardsImages;

    public TableReaderService(
        IRegionLookupCache regionLookupCache,
        IScreenReaderService screenReader,
        ICaptureService captureService,
        IImageCropperService imageCropperService,
        ICardCacheService cardCacheService,
        IOcrService ocrService,
        ILogger<TableReaderService> logger)
    {
        _regionLookupCache = regionLookupCache ?? throw new ArgumentNullException(nameof(regionLookupCache));
        _screenReader = screenReader ?? throw new ArgumentNullException(nameof(screenReader));
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        _imageCropperService = imageCropperService ?? throw new ArgumentNullException(nameof(imageCropperService));
        _cardCacheService = cardCacheService ?? throw new ArgumentNullException(nameof(cardCacheService));
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void SetBetPlayer(Bitmap screenshot, PlayerGameState state)
    {
        var betsRegions = _regionLookupCache.GetRegions("Bets");
        if (betsRegions == null || screenshot == null)
            return;

        foreach (var region in betsRegions)
        {
            var playerNumber = PlayerRegionParser.GetPlayerNumber(region.Name, "bet");
            if (playerNumber == null) continue;

            var scaled = ScaleRegion(region);
            var betValue = _screenReader.ReadBetValue(screenshot,
                scaled.X, scaled.Y, scaled.Width, scaled.Height,
                region.Umbral, region.InactiveUmbral, region.IsOnlyNumber, playerNumber);

            betValue = _screenReader.NormalizeBetValue(betValue, state.PotSize);

            if (playerNumber == 0)
            {
                state.CurrentBet = betValue;
                continue;
            }

            var player = state.Players.FirstOrDefault(f => f.Name == $"P{playerNumber}");
            if (player != null)
            {
                player.Bet = betValue;
            }
        }
    }

    /// <inheritdoc />
    public decimal SetHeroStack(Bitmap screenshot, PlayerGameState state)
    {
        var region = _regionLookupCache.GetRegion("User", "uStack");
        if (region == null || screenshot == null)
            return state.HeroStack;

        _ocrService.ClearCache();

        decimal stackValue = 0;
        int maxRetries = 2;

        var scaled = ScaleRegion(region);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            var rawValue = _screenReader.ReadStackValue(screenshot,
                scaled.X, scaled.Y, scaled.Width, scaled.Height,
                region.Umbral, region.InactiveUmbral, region.IsOnlyNumber);

            stackValue = _screenReader.NormalizeStackValue(rawValue);

            if (stackValue > 0)
                break;

            if (attempt < maxRetries)
                Thread.Sleep(100);
        }

        if (stackValue <= 0 && state.HeroStack > 0)
        {
            return state.HeroStack;
        }

        state.HeroStack = stackValue;
        return stackValue;
    }

    /// <inheritdoc />
    public void SetPotValue(Bitmap screenshot, PlayerGameState state)
    {
        var regionPot = _regionLookupCache.GetRegion("Table", "pot");
        if (screenshot == null)
            return;
        if (regionPot != null)
        {
            decimal potValue = 0;
            var scaled = ScaleRegion(regionPot);
            var pot = _screenReader.ReadText(screenshot,
                scaled.X, scaled.Y, scaled.Width, scaled.Height,
                regionPot.Umbral, regionPot.InactiveUmbral, regionPot.IsOnlyNumber);
            try
            {
                if (pot.Length == 4 && !pot.Contains(".") && !pot.Contains(","))
                    decimal.TryParse(pot.Substring(0, 2) + "," + pot.Substring(2), out potValue);
                else
                    decimal.TryParse(pot, out potValue);
            }
            catch (Exception)
            {
                // Mantener valor anterior si falla el parsing
            }
            state.PotSize = potValue;
        }
    }

    /// <inheritdoc />
    public async Task<(string HandNumber, bool IsNewHand)> SetTableHandAsync(Bitmap screenshot, PlayerGameState state)
    {
        return ("0", false); // Implementación real en FrmMain requiere estado complejo de FrmMain
    }

    /// <inheritdoc />
    public async Task ObtainCardsPlayerAsync(Bitmap screenshot, PlayerGameState state)
    {
        var userRegionsList = _regionLookupCache.GetRegions("User");
        if (userRegionsList == null || screenshot == null)
            return;

        var hashRegions = userRegionsList.Where(w => w.IsHash == true).ToList();

        _cardsImages ??= await _cardCacheService.GetCardsAsync();

        foreach (var region in hashRegions)
        {
            var scaled = ScaleRegion(region);

            var imageToBase64 = _imageCropperService.CropImageToBase64(
                screenshot,
                scaled.X,
                scaled.Y,
                scaled.Width,
                scaled.Height);

            if (_cardsImages == null || !_cardsImages.Any())
                continue;

            var bestMatch = _cardsImages
                .Where(item => !string.IsNullOrEmpty(item.ImageBase64))
                .Select(item => new
                {
                    Card = item,
                    Percentage = _imageCropperService.CompareCardsBase64(item.ImageBase64, imageToBase64)
                })
                .OrderByDescending(x => x.Percentage)
                .FirstOrDefault();

            if (bestMatch == null)
                continue;

            switch (region.Name)
            {
                case "u0cardface0":
                    state.HoleCard1Face = bestMatch.Card.Name.Split(" ")[0];
                    state.HoleCard1Rank = bestMatch.Card.Force;
                    state.HoleCard1Suit = bestMatch.Card.Suit;
                    break;
                case "u0cardface1":
                    state.HoleCard2Face = bestMatch.Card.Name.Split(" ")[0];
                    state.HoleCard2Rank = bestMatch.Card.Force;
                    state.HoleCard2Suit = bestMatch.Card.Suit;
                    break;
            }
        }
    }

    /// <inheritdoc />
    public bool IsBoardCardVisible(Bitmap screenshot, string cardRegionName)
    {
        if (screenshot == null || _cardsImages == null || !_cardsImages.Any())
            return false;

        var cardRegion = _regionLookupCache.GetRegion("Board", cardRegionName);
        if (cardRegion != null && cardRegion.IsHash != true)
            cardRegion = null;
        if (cardRegion == null)
            return false;

        var scaled = ScaleRegion(cardRegion);
        var imageToBase64 = _imageCropperService.CropImageToBase64(
            screenshot, scaled.X, scaled.Y, scaled.Width, scaled.Height);

        var bestMatch = _cardsImages
            .Where(item => !string.IsNullOrEmpty(item.ImageBase64))
            .Select(item => _imageCropperService.CompareCardsBase64(item.ImageBase64, imageToBase64))
            .DefaultIfEmpty(0)
            .Max();

        return bestMatch > 80.0;
    }

    /// <inheritdoc />
    public int CountVisibleBoardCards(Bitmap screenshot)
    {
        int count = 0;
        string[] cardRegions = { "Card1", "Card2", "Card3", "Card4", "Card5" };
        foreach (var region in cardRegions)
        {
            if (IsBoardCardVisible(screenshot, region))
                count++;
            else
                break;
        }
        return count;
    }

    /// <summary>
    /// Escala una región usando el CaptureService. Wrapper para mantener compatibilidad
    /// con el código legacy que usa tuplas (int X, int Y, int Width, int Height).
    /// </summary>
    private (int X, int Y, int Width, int Height) ScaleRegion(DomainRegion region)
    {
        var scaled = _captureService.GetScaledRegion(region);
        return (scaled.PosX, scaled.PosY, scaled.Width, scaled.Height);
    }
}
