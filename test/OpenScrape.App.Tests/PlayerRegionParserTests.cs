using OpenScrape.App.Helpers;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del helper extraído de <c>FrmMain.GetPlayerNumber</c>
/// (extract-frmmain-testable-logic Fase 1).
/// </summary>
[TestFixture]
public class PlayerRegionParserTests
{
    [Test]
    public void GetPlayerNumber_NombreEstandar_DevuelveNumero()
    {
        var result = PlayerRegionParser.GetPlayerNumber("p3bet", "bet");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void GetPlayerNumber_NumeroMultidigito_DevuelveNumeroCompleto()
    {
        var result = PlayerRegionParser.GetPlayerNumber("p11stack", "stack");
        Assert.That(result, Is.EqualTo(11));
    }

    [Test]
    public void GetPlayerNumber_SinExtraText_DevuelveNumero()
    {
        var result = PlayerRegionParser.GetPlayerNumber("p5");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void GetPlayerNumber_PrefijoDistinto_DevuelveNull()
    {
        var result = PlayerRegionParser.GetPlayerNumber("dealer_button");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPlayerNumber_StringVacio_DevuelveNull()
    {
        var result = PlayerRegionParser.GetPlayerNumber("");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPlayerNumber_StringNull_DevuelveNull()
    {
        var result = PlayerRegionParser.GetPlayerNumber(null);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPlayerNumber_ExtraTextNoCoincide_DevuelveNumero()
    {
        // El regex solo requiere que empiece con "p<digits>{extraText}"; si extraText
        // no aparece en el texto, el regex no matchea.
        var result = PlayerRegionParser.GetPlayerNumber("p3", "bet");
        Assert.That(result, Is.Null);
    }
}
