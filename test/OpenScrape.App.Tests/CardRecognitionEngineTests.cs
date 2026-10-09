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
}
