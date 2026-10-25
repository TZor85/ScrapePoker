using OpenScrape.App.Forms;

namespace OpenScrape.App.Tests.Forms;

/// <summary>
/// El tamaño sugerido es una fracción del pote. El overlay lo muestra como
/// múltiplo ("1x pot"), no como un número suelto que parezca fichas.
/// </summary>
[TestFixture]
public class SuggestedBetSizeTextTests
{
    [TestCase(1.0, "Bet: 1x pot")]
    [TestCase(0.5, "Bet: 0.5x pot")]
    [TestCase(0.67, "Bet: 0.67x pot")]
    [TestCase(0.33, "Bet: 0.33x pot")]
    [TestCase(1.25, "Bet: 1.25x pot")]
    public void FormatSuggestedBetSize_FraccionDelPote_MuestraMultiplo(double fraction, string expected)
    {
        Assert.That(FrmOverlay.FormatSuggestedBetSize(fraction), Is.EqualTo(expected));
    }
}
