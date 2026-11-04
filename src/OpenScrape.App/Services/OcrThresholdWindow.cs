namespace OpenScrape.App.Services;

/// <summary>
/// Ventana de umbrales que rodea al principal para las lecturas alternativas del OCR.
/// El umbral viaja como fraccion 0..1 (ProcessBitmap lo convierte con porcentaje * 255),
/// asi que el margen tambien tiene que ser una fraccion: sumarle o restarle 20 dejaba
/// las dos lecturas inutilizables.
/// </summary>
internal static class OcrThresholdWindow
{
    /// <summary>Margen de la ventana, en la misma escala 0..1 que el umbral.</summary>
    public const double Delta = 0.08;

    /// <summary>Devuelve los umbrales vecinos, recortados al rango valido.</summary>
    public static (double Lower, double Higher) Around(double umbral)
    {
        var centered = Math.Clamp(umbral, 0d, 1d);

        return (
            Math.Clamp(centered - Delta, 0d, 1d),
            Math.Clamp(centered + Delta, 0d, 1d));
    }
}
