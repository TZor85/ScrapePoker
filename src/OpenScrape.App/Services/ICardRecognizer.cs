using System.Drawing;
using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Services;

/// <summary>
/// Resultado del reconocimiento de una carta en una región de la imagen.
/// <c>Score</c> 0 indica ausencia de coincidencia; <c>Score</c> >= 90 indica coincidencia fiable.
/// </summary>
public readonly record struct CardMatch(CardDTO? Card, double Score);

/// <summary>
/// Reconoce cartas comparando una región recortada de la mesa contra las plantillas
/// precomputadas. Reemplaza el bucle <c>CropImageToBase64</c> + <c>CompareCardsBase64</c> x52.
/// </summary>
public interface ICardRecognizer
{
    /// <summary>
    /// Reconoce la mejor carta para la región indicada. Devuelve <c>CardMatch(null, 0)</c>
    /// si ninguna plantilla supera el umbral de similitud.
    /// </summary>
    CardMatch Recognize(Bitmap screenshot, int x, int y, int width, int height);

    /// <summary>
    /// Devuelve todos los candidatos con <c>Score &gt; 0</c> ordenados de mayor a menor.
    /// Útil para depuración e inspección de la shortlist.
    /// </summary>
    IReadOnlyList<CardMatch> RankCandidates(Bitmap screenshot, int x, int y, int width, int height);

    /// <summary>
    /// Indica si la región contiene una carta reconocible (<c>Score &gt; minScore</c>).
    /// </summary>
    bool IsVisible(Bitmap screenshot, int x, int y, int width, int height, double minScore = 80.0);
}
