using System.Drawing;
using System.Drawing.Imaging;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;
using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Tests;

[TestFixture]
public class CardRecognitionEngineTests
{
    [Test]
    public void PixelComparer_IdenticalBuffers_Returns100()
    {
        var buffer = new byte[4 * 4 * 4]; // 4x4
        for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)(i % 256);

        var score = PixelComparer.CalculateSimilarity(buffer, buffer, 4, 4);

        Assert.That(score, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public void PixelComparer_CompletelyDifferent_Returns0()
    {
        var a = new byte[4 * 4 * 4];
        var b = new byte[4 * 4 * 4];
        for (int i = 0; i < a.Length; i += 4)
        {
            a[i] = 0; a[i + 1] = 0; a[i + 2] = 0;
            b[i] = 255; b[i + 1] = 255; b[i + 2] = 255;
        }

        var score = PixelComparer.CalculateSimilarity(a, b, 4, 4);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void PixelComparer_HammingDistance_CountsDifferingBits()
    {
        Assert.That(PixelComparer.HammingDistance(0UL, 0UL), Is.EqualTo(0));
        Assert.That(PixelComparer.HammingDistance(0b1011UL, 0b1001UL), Is.EqualTo(1));
        Assert.That(PixelComparer.HammingDistance(ulong.MaxValue, 0UL), Is.EqualTo(64));
    }

    [Test]
    public void PixelComparer_EarlyTermination_Returns0_When_ManyDissimilarFirst()
    {
        // 100 pixels: first 50 completely different, last 50 identical.
        // At the 32-pixel checkpoint similarPixels=0 and remaining=68 < 90 → early exit.
        var a = new byte[100 * 4];
        var b = new byte[100 * 4];
        for (int p = 0; p < 100; p++)
        {
            int o = p * 4;
            if (p < 50)
            {
                a[o] = 0; a[o + 1] = 0; a[o + 2] = 0;
                b[o] = 255; b[o + 1] = 255; b[o + 2] = 255;
            }
            else
            {
                a[o] = 10; a[o + 1] = 20; a[o + 2] = 30;
                b[o] = 10; b[o + 1] = 20; b[o + 2] = 30;
            }
        }

        var score = PixelComparer.CalculateSimilarity(a, b, 10, 10);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void PixelComparer_At89Percent_Returns0_At91Percent_Returns91()
    {
        // 89 similar + 11 different → 89 < 90 → 0
        var below = BuildMixed(10, 10, similarPixels: 89);
        Assert.That(PixelComparer.CalculateSimilarity(below.Item1, below.Item2, 10, 10), Is.EqualTo(0.0));

        // 91 similar + 9 different → 91 >= 90 → 91
        var above = BuildMixed(10, 10, similarPixels: 91);
        Assert.That(PixelComparer.CalculateSimilarity(above.Item1, above.Item2, 10, 10), Is.EqualTo(91.0).Within(0.001));
    }

    [Test]
    public void PixelComparer_BufferShorterThanDeclared_Returns0()
    {
        Assert.That(PixelComparer.CalculateSimilarity(new byte[4], new byte[4], 100, 100), Is.EqualTo(0.0));
    }

    [Test]
    public void DHashCalculator_IdenticalBuffers_ReturnsSameHash()
    {
        var bgra = CreatePatternBgra(30, 40, seed: 1);

        var h1 = DHashCalculator.Compute(bgra, 30, 40);
        var h2 = DHashCalculator.Compute(bgra, 30, 40);

        Assert.That(h1, Is.EqualTo(h2));
    }

    [Test]
    public void DHashCalculator_DifferentPatterns_LargeDistance()
    {
        var ba = CreatePatternBgra(30, 40, seed: 1);
        var bb = CreatePatternBgra(30, 40, seed: 99);

        var distance = PixelComparer.HammingDistance(
            DHashCalculator.Compute(ba, 30, 40),
            DHashCalculator.Compute(bb, 30, 40));

        Assert.That(distance, Is.GreaterThan(0));
    }

    private static (byte[] A, byte[] B) BuildMixed(int width, int height, int similarPixels)
    {
        int total = width * height;
        var a = new byte[total * 4];
        var b = new byte[total * 4];
        for (int p = 0; p < total; p++)
        {
            int o = p * 4;
            if (p < similarPixels)
            {
                a[o] = 10; a[o + 1] = 20; a[o + 2] = 30;
                b[o] = 10; b[o + 1] = 20; b[o + 2] = 30;
            }
            else
            {
                a[o] = 0; a[o + 1] = 0; a[o + 2] = 0;
                b[o] = 255; b[o + 1] = 255; b[o + 2] = 255;
            }
        }
        return (a, b);
    }

    private static byte[] CreatePatternBgra(int width, int height, int seed)
    {
        var buffer = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int o = (y * width + x) * 4;
                byte v = (byte)((x * 7 + y * 13 + seed * 53) % 256);
                buffer[o] = (byte)((v + seed) % 256); // B
                buffer[o + 1] = (byte)(255 - v);       // G
                buffer[o + 2] = v;                      // R
                buffer[o + 3] = 255;                    // A
            }
        }
        return buffer;
    }

    private static Bitmap CreatePatternBitmap(int width, int height, int seed)
    {
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte v = (byte)((x * 7 + y * 13 + seed * 53) % 256);
                bmp.SetPixel(x, y, Color.FromArgb(255, v, (byte)(255 - v), (byte)((v + seed) % 256)));
            }
        }
        return bmp;
    }

    private static string ToBase64(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }
}
