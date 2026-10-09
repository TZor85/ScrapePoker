namespace OpenScrape.App.Services;

/// <summary>
/// Comparación pixel-a-pixel de buffers BGRA contiguos. Replica exactamente el
/// algoritmo de <see cref="ImageCropperService"/>: tolerancia 10 por canal R/G/B,
/// umbral de similitud 90% y terminación temprana cada 32 píxeles.
/// </summary>
public static class PixelComparer
{
    public const int DefaultTolerance = 10;
    public const double SimilarityThreshold = 90.0;
    private const int VerificationInterval = 32;

    /// <summary>
    /// Devuelve el porcentaje de similitud si es >= 90; en caso contrario 0.
    /// </summary>
    public static double CalculateSimilarity(byte[] pixels1, byte[] pixels2, int width, int height)
    {
        int totalPixels = width * height;
        if (totalPixels == 0)
            return 0;

        long requiredLength = (long)totalPixels * 4;
        if (pixels1.Length < requiredLength || pixels2.Length < requiredLength)
            return 0;

        int similarPixels = 0;
        int minThreshold = (int)(totalPixels * SimilarityThreshold / 100.0);
        int processedPixels = 0;

        int length = totalPixels * 4;
        for (int i = 0; i < length; i += 4)
        {
            if (IsPixelSimilar(pixels1, pixels2, i))
                similarPixels++;

            processedPixels++;

            if (processedPixels % VerificationInterval == 0)
            {
                int remaining = totalPixels - processedPixels;
                if (similarPixels + remaining < minThreshold)
                    return 0;
            }
        }

        double percentage = (double)similarPixels / totalPixels * 100;
        return percentage >= SimilarityThreshold ? percentage : 0;
    }

    /// <summary>
    /// Distancia Hamming entre dos hashes de 64 bits (algoritmo de Brian Kernighan).
    /// </summary>
    public static int HammingDistance(ulong hash1, ulong hash2)
    {
        ulong xor = hash1 ^ hash2;
        int count = 0;
        while (xor != 0)
        {
            xor &= xor - 1;
            count++;
        }
        return count;
    }

    private static bool IsPixelSimilar(byte[] pixels1, byte[] pixels2, int offset)
    {
        return Math.Abs(pixels1[offset + 2] - pixels2[offset + 2]) <= DefaultTolerance && // R
               Math.Abs(pixels1[offset + 1] - pixels2[offset + 1]) <= DefaultTolerance && // G
               Math.Abs(pixels1[offset] - pixels2[offset]) <= DefaultTolerance;           // B
    }
}
