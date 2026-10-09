using System.Drawing;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Services;

/// <summary>
/// Motor de reconocimiento de cartas. Pipeline por región: recorte directo LockBits →
/// dHash → shortlist por distancia Hamming (&lt;= 15) → comparación pixel-a-pixel exacta
/// sobre la shortlist → fallback a escaneo completo si la shortlist queda vacía.
/// La métrica final es siempre <see cref="PixelComparer.CalculateSimilarity"/>; el dHash
/// solo filtra candidatos, nunca decide el ganador.
/// </summary>
public sealed class CardRecognitionEngine : ICardRecognizer
{
    internal const int ShortlistMaxDistance = 15;

    private readonly CardTemplateIndex _templateIndex;
    private readonly IMetricsCollector? _metrics;

    public CardRecognitionEngine(CardTemplateIndex templateIndex, IMetricsCollector? metrics = null)
    {
        _templateIndex = templateIndex ?? throw new ArgumentNullException(nameof(templateIndex));
        _metrics = metrics;
    }

    public CardMatch Recognize(Bitmap screenshot, int x, int y, int width, int height)
    {
        var candidates = RankCandidates(screenshot, x, y, width, height);
        return candidates.Count > 0 ? candidates[0] : new CardMatch(null, 0);
    }

    public IReadOnlyList<CardMatch> RankCandidates(Bitmap screenshot, int x, int y, int width, int height)
    {
        ScopedMeasurement? measurement = _metrics?.Measure(TelemetryCategories.OcrCards);
        try
        {
            var regionPixels = RegionPixelExtractor.ExtractBgra(screenshot, x, y, width, height);
            if (regionPixels == null)
                return Array.Empty<CardMatch>();

            var templates = _templateIndex.Templates;
            ulong regionHash = DHashCalculator.Compute(regionPixels, width, height);

            var shortlist = new List<CardTemplate>();
            foreach (var template in templates)
            {
                if (template.Width != width || template.Height != height)
                    continue;
                if (PixelComparer.HammingDistance(template.DHash, regionHash) <= ShortlistMaxDistance)
                    shortlist.Add(template);
            }

            var toScore = shortlist.Count > 0 ? shortlist : BuildFallback(templates, width, height);

            var matches = new List<CardMatch>(toScore.Count);
            foreach (var template in toScore)
            {
                double score = PixelComparer.CalculateSimilarity(template.Bgra, regionPixels, width, height);
                if (score > 0)
                    matches.Add(new CardMatch(template.Card, score));
            }

            matches.Sort((a, b) => b.Score.CompareTo(a.Score));
            return matches;
        }
        finally
        {
            measurement?.Dispose();
        }
    }

    public bool IsVisible(Bitmap screenshot, int x, int y, int width, int height, double minScore = 80.0)
    {
        return Recognize(screenshot, x, y, width, height).Score > minScore;
    }

    private static List<CardTemplate> BuildFallback(IReadOnlyList<CardTemplate> templates, int width, int height)
    {
        var fallback = new List<CardTemplate>(templates.Count);
        foreach (var template in templates)
        {
            if (template.Width == width && template.Height == height)
                fallback.Add(template);
        }
        return fallback;
    }
}
