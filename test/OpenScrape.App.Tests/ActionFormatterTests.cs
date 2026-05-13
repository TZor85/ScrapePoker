using NUnit.Framework.Constraints;

using OpenScrape.App.Entities;
using OpenScrape.App.Services;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del ActionFormatter extraído de FrmMain.EnrichActionWithBBAmount
/// (extract-frmmain-testable-logic Fase 2).
/// </summary>
[TestFixture]
public class ActionFormatterTests
{
    private ActionFormatter _formatter = null!;

    [SetUp]
    public void SetUp() => _formatter = new ActionFormatter();

    private static Player Villain(decimal bet) => new() { Name = "P1", Bet = bet };

    // Helper para aceptar ambas culturas (es-ES usa ',', en-US usa '.').
    private static IResolveConstraint IsOneOf(string a, string b) =>
        new NUnit.Framework.Constraints.EqualConstraint(a).Or.EqualTo(b);

    // ─── Sin cambios ──────────────────────────────────────────────────────

    [Test]
    public void EnrichActionWithBBAmount_SinX_DevuelveOriginal()
    {
        var result = _formatter.EnrichActionWithBBAmount("Check", Array.Empty<Player>(), 0.5m);
        Assert.That(result, Is.EqualTo("Check"));
    }

    [Test]
    public void EnrichActionWithBBAmount_MultiplicadorCero_DevuelveOriginal()
    {
        var result = _formatter.EnrichActionWithBBAmount("Raise x0", Array.Empty<Player>(), 0.5m);
        Assert.That(result, Is.EqualTo("Raise x0"));
    }

    [Test]
    public void EnrichActionWithBBAmount_MultiplicadorInvalido_DevuelveOriginal()
    {
        var result = _formatter.EnrichActionWithBBAmount("Raise xabc", Array.Empty<Player>(), 0.5m);
        Assert.That(result, Is.EqualTo("Raise xabc"));
    }

    [Test]
    public void EnrichActionWithBBAmount_ActionVacia_DevuelveOriginal()
    {
        var result = _formatter.EnrichActionWithBBAmount("", Array.Empty<Player>(), 0.5m);
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void EnrichActionWithBBAmount_ActionSoloX_DevuelveOriginal()
    {
        // "x" final sin nada que parsear detrás
        var result = _formatter.EnrichActionWithBBAmount("Raise x", Array.Empty<Player>(), 0.5m);
        Assert.That(result, Is.EqualTo("Raise x"));
    }

    // ─── Cálculo normal ───────────────────────────────────────────────────

    [Test]
    public void EnrichActionWithBBAmount_OpenRaiseSinVillano_UsaBigBlindComoBase()
    {
        // x2.4 × 0.5 = 1.2, totalBB = 1.2/0.5 = 2.4 BB
        var result = _formatter.EnrichActionWithBBAmount("Open Raise x2.4", Array.Empty<Player>(), 0.5m);
        Assert.That(result, IsOneOf("Open Raise x2.4 (2,4BB)", "Open Raise x2.4 (2.4BB)"));
    }

    [Test]
    public void EnrichActionWithBBAmount_3BetSobreVillano_UsaMayorBetComoBase()
    {
        // Villano apuesta 2.5, x6 → 15, totalBB = 15/0.5 = 30
        var result = _formatter.EnrichActionWithBBAmount("3Bet x6", new[] { Villain(2.5m) }, 0.5m);
        Assert.That(result, IsOneOf("3Bet x6 (30BB)", "3Bet x6 (30BB)"));
        Assert.That(result, Does.Contain("30BB"));
    }

    [Test]
    public void EnrichActionWithBBAmount_MultiplesVillanos_TomaMayorBet()
    {
        var villains = new[] { Villain(1.0m), Villain(3.0m), Villain(2.0m) };
        // x2 × 3.0 = 6.0, BB 0.5 → 12 BB
        var result = _formatter.EnrichActionWithBBAmount("4Bet x2", villains, 0.5m);
        Assert.That(result, Does.Contain("12BB"));
    }

    [Test]
    public void EnrichActionWithBBAmount_BetDecimalNoRedondo_RedondeaA1Decimal()
    {
        // x2.7 × 0.5 = 1.35, totalBB = 1.35/0.5 = 2.7
        var result = _formatter.EnrichActionWithBBAmount("Raise x2.7", Array.Empty<Player>(), 0.5m);
        Assert.That(result, IsOneOf("Raise x2.7 (2,7BB)", "Raise x2.7 (2.7BB)"));
    }

    // ─── Big blind fallback ───────────────────────────────────────────────

    [Test]
    public void EnrichActionWithBBAmount_BigBlindCero_UsaFallback()
    {
        // BB fallback 0.5, x2 × 0.5 = 1.0 → 2 BB
        var result = _formatter.EnrichActionWithBBAmount("Open Raise x2", Array.Empty<Player>(), 0m);
        Assert.That(result, Does.Contain("2BB"));
    }

    [Test]
    public void EnrichActionWithBBAmount_BigBlindNegativo_UsaFallback()
    {
        var result = _formatter.EnrichActionWithBBAmount("Open Raise x2", Array.Empty<Player>(), -1m);
        Assert.That(result, Does.Contain("2BB"));
    }

    // ─── Comportamiento con xIndex al final ───────────────────────────────

    [Test]
    public void EnrichActionWithBBAmount_MultiplicadorEntero_Funciona()
    {
        // x3 × 0.5 = 1.5 → 3 BB
        var result = _formatter.EnrichActionWithBBAmount("Open Raise x3", Array.Empty<Player>(), 0.5m);
        Assert.That(result, Does.Contain("3BB"));
    }
}
