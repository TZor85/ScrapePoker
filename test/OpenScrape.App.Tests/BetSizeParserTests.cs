using OpenScrape.DecisionMaker.Postflop;

namespace OpenScrape.App.Tests;

/// <summary>
/// El motor postflop decide el tamaño dentro del texto de la acción
/// ("Bet Pot (Value)", "Bet 1/2 (C-Bet)"). El overlay necesita ese tamaño como
/// número: si el texto no lo dice, no hay número que mostrar.
/// </summary>
[TestFixture]
public class BetSizeParserTests
{
    [TestCase("Bet Pot (Value)", 1.0)]
    [TestCase("Bet 3/4 (Value)", 0.75)]
    [TestCase("Bet 2/3 (Value)", 0.67)]
    [TestCase("Bet 1/2 (C-Bet)", 0.5)]
    [TestCase("Bet 1/3 (Bluff)", 0.33)]
    [TestCase("Bet 1/4 (Bluff)", 0.25)]
    [TestCase("Bet 1.25x (Value)", 1.25)]
    [TestCase("Raise Pot", 1.0)]
    public void FromAction_AccionConTamano_DevuelveLaFraccionDelPote(string action, double expected)
    {
        Assert.That(BetSizeParser.FromAction(action), Is.EqualTo(expected).Within(0.001));
    }

    [TestCase("Check")]
    [TestCase("Call")]
    [TestCase("Fold")]
    [TestCase("Bet 3x (Value)")]
    [TestCase("")]
    [TestCase(null)]
    public void FromAction_AccionSinTamano_DevuelveNull(string? action)
    {
        Assert.That(BetSizeParser.FromAction(action), Is.Null);
    }
}
