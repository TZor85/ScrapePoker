using OpenScrape.App.Services;

namespace OpenScrape.App.Tests;

/// <summary>
/// La equity efectiva que consume el motor debe quedar siempre dentro de [0, 100].
/// El ajuste de c-bet y la penalidad por peligro pueden empujarla fuera del rango,
/// y el motor rechaza el input en ese caso.
/// </summary>
[TestFixture]
public class EquityClampTests
{
    [Test]
    public void ClampEquity_PorEncimaDeCien_DevuelveCien()
    {
        // Arrange
        const double equity = 102.5;

        // Act
        var resultado = GameCoordinator.ClampEquity(equity);

        // Assert
        Assert.That(resultado, Is.EqualTo(100));
    }

    [Test]
    public void ClampEquity_PorDebajoDeCero_DevuelveCero()
    {
        // Arrange
        const double equity = -3.2;

        // Act
        var resultado = GameCoordinator.ClampEquity(equity);

        // Assert
        Assert.That(resultado, Is.EqualTo(0));
    }

    [Test]
    public void ClampEquity_DentroDelRango_NoLaModifica()
    {
        // Arrange
        const double equity = 52.3;

        // Act
        var resultado = GameCoordinator.ClampEquity(equity);

        // Assert
        Assert.That(resultado, Is.EqualTo(52.3));
    }
}
