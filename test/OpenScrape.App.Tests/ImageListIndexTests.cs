using OpenScrape.App.Helpers;

namespace OpenScrape.App.Tests;

[TestFixture]
public class ImageListIndexTests
{
    private static readonly string[] Imagenes =
    [
        @"C:\samples\settings\game_1783944253198.png",
        @"C:\samples\settings\game_1783944299535.png",
        @"C:\samples\settings\game_1783944810880.png",
        @"C:\samples\settings\game_1783944813113.png"
    ];

    [Test]
    public void FindIndex_ArchivoPresente_DevuelveSuPosicion()
    {
        // Arrange
        var seleccionado = @"C:\samples\settings\game_1783944810880.png";

        // Act
        var indice = ImageListIndex.FindIndex(Imagenes, seleccionado);

        // Assert
        Assert.That(indice, Is.EqualTo(2));
    }

    [Test]
    public void FindIndex_RutaConMayusculasDistintas_EncuentraElArchivo()
    {
        // Arrange: el dialogo puede devolver la ruta con otra capitalizacion
        var seleccionado = @"c:\SAMPLES\settings\GAME_1783944813113.PNG";

        // Act
        var indice = ImageListIndex.FindIndex(Imagenes, seleccionado);

        // Assert
        Assert.That(indice, Is.EqualTo(3));
    }

    [Test]
    public void FindIndex_ArchivoAusente_DevuelveCero()
    {
        // Arrange
        var seleccionado = @"C:\samples\settings\game_otro.png";

        // Act
        var indice = ImageListIndex.FindIndex(Imagenes, seleccionado);

        // Assert
        Assert.That(indice, Is.EqualTo(0), "Sin coincidencia se cae al primer elemento de la lista");
    }

    [Test]
    public void FindIndex_ListaVacia_DevuelveCero()
    {
        // Act
        var indice = ImageListIndex.FindIndex(Array.Empty<string>(), @"C:\samples\game_1.png");

        // Assert
        Assert.That(indice, Is.EqualTo(0));
    }

    [Test]
    public void FindIndex_SinSeleccion_DevuelveCero()
    {
        // Act
        var indiceNulo = ImageListIndex.FindIndex(Imagenes, null);
        var indiceBlanco = ImageListIndex.FindIndex(Imagenes, "   ");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(indiceNulo, Is.EqualTo(0));
            Assert.That(indiceBlanco, Is.EqualTo(0));
        });
    }
}
