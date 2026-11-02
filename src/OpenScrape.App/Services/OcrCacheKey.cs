namespace OpenScrape.App.Services;

/// <summary>
/// Clave de la caché de OCR. Incluye el umbral y el modo "solo números" porque el
/// mismo recorte produce textos distintos según cómo se binarice: cachear sólo por
/// píxeles hacía que la segunda lectura recibiera el resultado de la primera.
/// </summary>
internal readonly record struct OcrCacheKey(ulong Hash, int Threshold, bool OnlyNumber)
{
    /// <summary>
    /// El umbral llega como fracción 0..1 y el binarizado lo usa en escala 0..255,
    /// así que se cuantiza igual para que dos lecturas equivalentes compartan clave.
    /// </summary>
    public static OcrCacheKey From(ulong hash, double umbral, bool onlyNumber)
    {
        var niveles = (int)Math.Round(Math.Clamp(umbral, 0d, 1d) * 255d, MidpointRounding.ToEven);
        return new OcrCacheKey(hash, niveles, onlyNumber);
    }
}
