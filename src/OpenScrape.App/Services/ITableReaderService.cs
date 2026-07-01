using System.Drawing;
using OpenScrape.App.Entities;

namespace OpenScrape.App.Services;

/// <summary>
/// Contrato para el servicio de lectura OCR de la mesa de póker.
/// Extrae SetBetPlayer, SetHeroStack, SetPotValue, SetTableHand,
/// ObtainCardsPlayerAsync, IsBoardCardVisible y CountVisibleBoardCards de FrmMain.
/// </summary>
public interface ITableReaderService
{
    /// <summary>
    /// Lee las regiones de apuesta vía OCR y actualiza PlayerGameState con los valores parseados.
    /// </summary>
    void SetBetPlayer(Bitmap screenshot, PlayerGameState state);

    /// <summary>
    /// Lee el stack del héroe vía OCR y retorna el valor detectado.
    /// </summary>
    decimal SetHeroStack(Bitmap screenshot, PlayerGameState state);

    /// <summary>
    /// Lee el valor del bote vía OCR y actualiza PlayerGameState.PotSize.
    /// </summary>
    void SetPotValue(Bitmap screenshot, PlayerGameState state);

    /// <summary>
    /// Lee el número de mano de la mesa y detecta cambios de mano.
    /// Retorna el número de mano detectado y si es una nueva mano.
    /// </summary>
    Task<(string HandNumber, bool IsNewHand)> SetTableHandAsync(Bitmap screenshot, PlayerGameState state);

    /// <summary>
    /// Lee las cartas del héroe usando ImageCropperService y CardCacheService,
    /// y actualiza PlayerGameState.HoleCard1Face/HoleCard2Face.
    /// </summary>
    Task ObtainCardsPlayerAsync(Bitmap screenshot, PlayerGameState state);

    /// <summary>
    /// Verifica si una carta del board (Card1 a Card5) es visible comparando la imagen
    /// de la región contra las cartas conocidas.
    /// </summary>
    bool IsBoardCardVisible(Bitmap screenshot, string cardRegionName);

    /// <summary>
    /// Cuenta cuántas cartas del board son realmente visibles (Card1 a Card5).
    /// Las cartas son secuenciales: si Card3 no es visible, Card4/5 tampoco.
    /// </summary>
    int CountVisibleBoardCards(Bitmap screenshot);
}
