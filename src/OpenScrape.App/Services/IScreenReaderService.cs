using System.Drawing;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio de lectura OCR de pantalla. Encapsula toda la lógica de lectura,
/// preprocesamiento, consenso multi-lectura y normalización de valores OCR.
/// </summary>
public interface IScreenReaderService
{
    /// <summary>
    /// Lee el nombre de un jugador con doble lectura y consenso.
    /// </summary>
    string ReadPlayerName(Image screenshot, int x, int y, int w, int h, double umbral, double inactiveUmbral);

    /// <summary>
    /// Lee un valor de apuesta con 3 lecturas (2 preprocesadas + 1 directa) y consenso.
    /// </summary>
    decimal ReadBetValue(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber, int? playerNum = null);

    /// <summary>
    /// Lee un valor de stack con 3 lecturas (2 preprocesadas + 1 directa) y consenso.
    /// </summary>
    decimal ReadStackValue(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber);

    /// <summary>
    /// Lee el número de mano con 3 lecturas y consenso.
    /// </summary>
    string ReadHandNumber(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber);

    /// <summary>
    /// Lee texto genérico con retry en confianza baja.
    /// </summary>
    string ReadText(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber);

    /// <summary>
    /// Lee texto probando múltiples umbrales (para detectar "SIT OUT").
    /// </summary>
    string ReadTextWithMultipleThresholds(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber);

    /// <summary>
    /// Normaliza valor de apuesta corrigiendo artefactos OCR (separador decimal perdido, "8" espurio).
    /// </summary>
    decimal NormalizeBetValue(decimal rawValue, decimal potSize = 0);

    /// <summary>
    /// Normaliza valor de stack corrigiendo artefactos OCR.
    /// </summary>
    decimal NormalizeStackValue(decimal rawValue);
}
