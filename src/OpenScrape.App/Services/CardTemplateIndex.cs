using System.Drawing;
using Microsoft.Extensions.Logging;
using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Services;

/// <summary>
/// Índice de las plantillas de carta precomputadas (BGRA + dHash + dimensiones).
/// Se inicializa una sola vez desde <see cref="ICardCacheService"/> (Marten).
/// </summary>
public sealed class CardTemplateIndex
{
    private readonly ICardCacheService _cardCacheService;
    private readonly ILogger<CardTemplateIndex>? _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile IReadOnlyList<CardTemplate>? _templates;

    public CardTemplateIndex(ICardCacheService cardCacheService, ILogger<CardTemplateIndex>? logger = null)
    {
        _cardCacheService = cardCacheService ?? throw new ArgumentNullException(nameof(cardCacheService));
        _logger = logger;
    }

    public bool IsInitialized => _templates != null;

    public IReadOnlyList<CardTemplate> Templates
    {
        get
        {
            if (_templates == null)
                throw new InvalidOperationException("CardTemplateIndex no inicializado. Llame a InitializeAsync primero.");
            return _templates;
        }
    }

    public async Task InitializeAsync()
    {
        if (_templates != null)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_templates != null)
                return;

            var cards = await _cardCacheService.GetCardsAsync();
            var templates = new List<CardTemplate>(cards.Count);

            foreach (var card in cards)
            {
                var template = BuildTemplate(card);
                if (template != null)
                    templates.Add(template);
            }

            _templates = templates;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private CardTemplate? BuildTemplate(CardDTO card)
    {
        if (string.IsNullOrEmpty(card.ImageBase64))
            return null;

        try
        {
            var bytes = DecodeBase64(card.ImageBase64);
            using var ms = new MemoryStream(bytes, writable: false);
            using var image = Image.FromStream(ms);
            using var bitmap = new Bitmap(image);

            var bgra = RegionPixelExtractor.ExtractBgra(bitmap, 0, 0, bitmap.Width, bitmap.Height);
            if (bgra == null)
                return null;

            ulong dHash = DHashCalculator.Compute(bgra, bitmap.Width, bitmap.Height);
            return new CardTemplate(card, bgra, bitmap.Width, bitmap.Height, dHash);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or System.Runtime.InteropServices.ExternalException)
        {
            _logger?.LogWarning(ex, "No se pudo construir la plantilla para la carta {CardName}", card.Name);
            return null;
        }
    }

    private static byte[] DecodeBase64(string base64)
    {
        ReadOnlySpan<char> span = base64.AsSpan();
        int commaIndex = span.IndexOf(',');
        if (commaIndex >= 0)
            span = span.Slice(commaIndex + 1);
        return Convert.FromBase64String(span.ToString());
    }
}
