using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenScrape.App.Helpers;

public class ImagePreprocessorHelper
{
    /// <summary>
    //// <summary>
    /// <summary>
    /// Preprocesa una región específica de una imagen para optimizarla para OCR
    /// </summary>
    public Image PreprocessImageForOCR(Image originalImage, Rectangle region)
    {
        using (var bitmap = new Bitmap(originalImage))
        {
            if (region.X < 0 || region.Y < 0 ||
                region.Right > bitmap.Width ||
                region.Bottom > bitmap.Height)
            {
                throw new ArgumentException("La región especificada está fuera de los límites de la imagen.");
            }

            var regionBitmap = new Bitmap(region.Width, region.Height);
            using (var g = Graphics.FromImage(regionBitmap))
            {
                g.DrawImage(bitmap,
                    new Rectangle(0, 0, region.Width, region.Height),
                    region,
                    GraphicsUnit.Pixel);
            }

            return ProcessImage(regionBitmap);
        }
    }

    private Bitmap ProcessImage(Bitmap original)
    {
        var processed = (Bitmap)original.Clone();

        if (original.Width < 1000 && original.Height < 1000)
        {
            processed = ResizeImage(processed, 2.0f);
        }

        processed = FastGrayscale(processed);

        if (HasSignificantNoise(processed))
        {
            processed = FastMedianFilter(processed);
        }

        processed = FastContrast(processed, 1.5f);

        if (DetectSkew(processed) > 0.5)
        {
            processed = Deskew(processed);
        }

        processed = FastBinarize(processed);
        return processed;
    }

    private Bitmap FastGrayscale(Bitmap source)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format8bppIndexed);

        // Configurar paleta de grises
        ColorPalette grayPalette = result.Palette;
        for (int i = 0; i < 256; i++)
            grayPalette.Entries[i] = Color.FromArgb(i, i, i);
        result.Palette = grayPalette;

        BitmapData srcData = source.LockBits(
            new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        BitmapData resultData = result.LockBits(
            new Rectangle(0, 0, result.Width, result.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format8bppIndexed);

        int srcStride = srcData.Stride;
        int resultStride = resultData.Stride;

        byte[] srcBytes = new byte[srcStride * source.Height];
        byte[] resultBytes = new byte[resultStride * result.Height];

        Marshal.Copy(srcData.Scan0, srcBytes, 0, srcBytes.Length);

        Parallel.For(0, source.Height, y =>
        {
            for (int x = 0; x < source.Width; x++)
            {
                int srcOffset = y * srcStride + x * 4;
                int resultOffset = y * resultStride + x;

                byte blue = srcBytes[srcOffset];
                byte green = srcBytes[srcOffset + 1];
                byte red = srcBytes[srcOffset + 2];

                resultBytes[resultOffset] = (byte)((red * 0.3) + (green * 0.59) + (blue * 0.11));
            }
        });

        Marshal.Copy(resultBytes, 0, resultData.Scan0, resultBytes.Length);

        source.UnlockBits(srcData);
        result.UnlockBits(resultData);

        return result;
    }

    private Bitmap FastMedianFilter(Bitmap source)
    {
        var result = new Bitmap(source.Width, source.Height, source.PixelFormat);

        BitmapData srcData = source.LockBits(
            new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.ReadOnly,
            source.PixelFormat);

        BitmapData resultData = result.LockBits(
            new Rectangle(0, 0, result.Width, result.Height),
            ImageLockMode.WriteOnly,
            result.PixelFormat);

        int stride = srcData.Stride;
        byte[] srcBytes = new byte[stride * source.Height];
        byte[] resultBytes = new byte[stride * result.Height];

        Marshal.Copy(srcData.Scan0, srcBytes, 0, srcBytes.Length);

        Parallel.For(1, source.Height - 1, y =>
        {
            for (int x = 1; x < source.Width - 1; x++)
            {
                var values = new byte[9];
                int idx = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        values[idx++] = srcBytes[(y + dy) * stride + (x + dx)];
                    }
                }

                Array.Sort(values);
                resultBytes[y * stride + x] = values[4];
            }
        });

        Marshal.Copy(resultBytes, 0, resultData.Scan0, resultBytes.Length);

        source.UnlockBits(srcData);
        result.UnlockBits(resultData);

        return result;
    }

    private Bitmap FastContrast(Bitmap source, float factor)
    {
        var result = new Bitmap(source.Width, source.Height, source.PixelFormat);

        // Precalcular tabla de búsqueda para el contraste
        byte[] contrastLookup = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            double value = i / 255.0;
            value = ((value - 0.5) * factor) + 0.5;
            value = Math.Max(0, Math.Min(1, value)) * 255;
            contrastLookup[i] = (byte)value;
        }

        BitmapData srcData = source.LockBits(
            new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.ReadOnly,
            source.PixelFormat);

        BitmapData resultData = result.LockBits(
            new Rectangle(0, 0, result.Width, result.Height),
            ImageLockMode.WriteOnly,
            result.PixelFormat);

        int stride = srcData.Stride;
        byte[] srcBytes = new byte[stride * source.Height];
        byte[] resultBytes = new byte[stride * result.Height];

        Marshal.Copy(srcData.Scan0, srcBytes, 0, srcBytes.Length);

        Parallel.For(0, source.Height, y =>
        {
            for (int x = 0; x < source.Width; x++)
            {
                int offset = y * stride + x;
                resultBytes[offset] = contrastLookup[srcBytes[offset]];
            }
        });

        Marshal.Copy(resultBytes, 0, resultData.Scan0, resultBytes.Length);

        source.UnlockBits(srcData);
        result.UnlockBits(resultData);

        return result;
    }

    private Bitmap FastBinarize(Bitmap source)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format1bppIndexed);
        int threshold = FastCalculateOtsuThreshold(source);

        BitmapData srcData = source.LockBits(
            new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.ReadOnly,
            source.PixelFormat);

        BitmapData resultData = result.LockBits(
            new Rectangle(0, 0, result.Width, result.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format1bppIndexed);

        int srcStride = srcData.Stride;
        int resultStride = resultData.Stride;

        byte[] srcBytes = new byte[srcStride * source.Height];
        byte[] resultBytes = new byte[resultStride * result.Height];

        Marshal.Copy(srcData.Scan0, srcBytes, 0, srcBytes.Length);

        Parallel.For(0, source.Height, y =>
        {
            for (int x = 0; x < source.Width; x += 8)
            {
                byte resultByte = 0;
                for (int bit = 0; bit < 8 && (x + bit) < source.Width; bit++)
                {
                    if (srcBytes[y * srcStride + x + bit] > threshold)
                        resultByte |= (byte)(0x80 >> bit);
                }
                resultBytes[y * resultStride + (x >> 3)] = resultByte;
            }
        });

        Marshal.Copy(resultBytes, 0, resultData.Scan0, resultBytes.Length);

        source.UnlockBits(srcData);
        result.UnlockBits(resultData);

        return result;
    }

    private int FastCalculateOtsuThreshold(Bitmap source)
    {
        int[] histogram = new int[256];

        BitmapData srcData = source.LockBits(
            new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.ReadOnly,
            source.PixelFormat);

        int stride = srcData.Stride;
        byte[] bytes = new byte[stride * source.Height];
        Marshal.Copy(srcData.Scan0, bytes, 0, bytes.Length);

        Parallel.For(0, source.Height, y =>
        {
            for (int x = 0; x < source.Width; x++)
            {
                byte pixel = bytes[y * stride + x];
                lock (histogram)
                {
                    histogram[pixel]++;
                }
            }
        });

        source.UnlockBits(srcData);

        int total = source.Width * source.Height;
        float sum = 0;
        for (int i = 0; i < 256; i++)
            sum += i * histogram[i];

        float sumB = 0;
        int wB = 0;
        float maxVariance = 0;
        int threshold = 0;

        for (int i = 0; i < 256; i++)
        {
            wB += histogram[i];
            if (wB == 0) continue;

            int wF = total - wB;
            if (wF == 0) break;

            sumB += i * histogram[i];
            float mB = sumB / wB;
            float mF = (sum - sumB) / wF;

            float variance = wB * wF * (mB - mF) * (mB - mF);
            if (variance > maxVariance)
            {
                maxVariance = variance;
                threshold = i;
            }
        }

        return threshold;
    }

    private bool HasSignificantNoise(Bitmap image)
    {
        const int sampleSize = 100;
        int noiseCount = 0;

        BitmapData data = image.LockBits(
            new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadOnly,
            image.PixelFormat);

        int stride = data.Stride;
        byte[] bytes = new byte[stride * image.Height];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

        for (int y = 1; y < image.Height - 1; y += image.Height / sampleSize)
        {
            for (int x = 1; x < image.Width - 1; x += image.Width / sampleSize)
            {
                byte current = bytes[y * stride + x];
                byte left = bytes[y * stride + (x - 1)];
                byte right = bytes[y * stride + (x + 1)];

                if (Math.Abs(current - left) > 50 || Math.Abs(current - right) > 50)
                {
                    noiseCount++;
                }
            }
        }

        image.UnlockBits(data);
        return noiseCount > (sampleSize * sampleSize / 10);
    }

    private double DetectSkew(Bitmap image)
    {
        int edgeCount = 0;
        double totalAngle = 0;

        BitmapData data = image.LockBits(
            new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadOnly,
            image.PixelFormat);

        int stride = data.Stride;
        byte[] bytes = new byte[stride * image.Height];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

        for (int y = 10; y < image.Height - 10; y += image.Height / 20)
        {
            int lastEdge = -1;
            for (int x = 0; x < image.Width; x++)
            {
                byte current = bytes[y * stride + x];
                if (x > 0)
                {
                    byte prev = bytes[y * stride + (x - 1)];
                    if (Math.Abs(current - prev) > 40)
                    {
                        if (lastEdge != -1)
                        {
                            double angle = Math.Atan2(y - lastEdge, x);
                            totalAngle += angle;
                            edgeCount++;
                        }
                        lastEdge = y;
                    }
                }
            }
        }

        image.UnlockBits(data);
        return edgeCount > 0 ? Math.Abs(totalAngle / edgeCount) : 0;
    }

    private Bitmap ResizeImage(Bitmap image, float scale)
    {
        int width = (int)(image.Width * scale);
        int height = (int)(image.Height * scale);

        var resized = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(resized))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(image, 0, 0, width, height);
        }
        return resized;
    }

    private Bitmap Deskew(Bitmap image)
    {
        // Implementación del algoritmo de Hough para detectar y corregir la inclinación
        return image;
    }
}