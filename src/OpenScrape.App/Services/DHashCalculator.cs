namespace OpenScrape.App.Services;

/// <summary>
/// Calcula un dHash de 64 bits a partir de un buffer BGRA contiguo. Hace un downsample
/// por promedio de cajas a 9x8 en escala de grises y compara píxeles horizontalmente
/// adyacentes. La misma implementación se usa para plantillas y regiones, garantizando
/// que imágenes idénticas produzcan distancia Hamming 0.
/// </summary>
public static class DHashCalculator
{
    private const int TargetWidth = 9;
    private const int TargetHeight = 8;

    public static ulong Compute(byte[] bgra, int width, int height)
    {
        if (width <= 0 || height <= 0 || bgra.Length < (long)width * height * 4)
            return 0;

        var gray = new int[TargetWidth * TargetHeight];

        for (int ty = 0; ty < TargetHeight; ty++)
        {
            int y0 = ty * height / TargetHeight;
            int y1 = (ty + 1) * height / TargetHeight;
            if (y1 <= y0) y1 = Math.Min(height, y0 + 1);

            for (int tx = 0; tx < TargetWidth; tx++)
            {
                int x0 = tx * width / TargetWidth;
                int x1 = (tx + 1) * width / TargetWidth;
                if (x1 <= x0) x1 = Math.Min(width, x0 + 1);

                long sum = 0;
                long count = 0;
                for (int y = y0; y < y1; y++)
                {
                    int rowOffset = y * width * 4;
                    for (int x = x0; x < x1; x++)
                    {
                        int offset = rowOffset + x * 4;
                        sum += (bgra[offset] + bgra[offset + 1] + bgra[offset + 2]) / 3;
                        count++;
                    }
                }

                gray[ty * TargetWidth + tx] = count > 0 ? (int)(sum / count) : 0;
            }
        }

        ulong hash = 0;
        int bitIndex = 0;
        for (int y = 0; y < TargetHeight; y++)
        {
            for (int x = 0; x < TargetWidth - 1; x++)
            {
                if (gray[y * TargetWidth + x] > gray[y * TargetWidth + x + 1])
                    hash |= 1UL << bitIndex;
                bitIndex++;
            }
        }

        return hash;
    }
}
