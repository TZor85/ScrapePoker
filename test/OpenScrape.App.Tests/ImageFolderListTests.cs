using OpenScrape.App.Helpers;

namespace OpenScrape.App.Tests;

/// <summary>
/// La lista de imágenes para navegar tiene que salir de la carpeta del archivo
/// elegido. Cortar el path por "game" matcheaba el segmento "games" de la ruta y
/// devolvía los PNG del nivel de arriba: el visor mostraba otra imagen, los botones
/// de navegación quedaban clavados y la captura no encontraba la mesa.
/// </summary>
[TestFixture]
public class ImageFolderListTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "openscrape-imagefolder-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Test]
    public void Resolve_ArchivoEnCarpetaDeSesion_DevuelveSoloLosPngDeEsaCarpeta()
    {
        var samples = Directory.CreateDirectory(Path.Combine(_root, "samples")).FullName;
        File.WriteAllBytes(Path.Combine(samples, "game_viejo.png"), []);

        var session = Directory.CreateDirectory(
            Path.Combine(samples, "games", "20261028_Game", "0268593765")).FullName;
        var primero = Path.Combine(session, "game_1.png");
        var segundo = Path.Combine(session, "game_2.png");
        File.WriteAllBytes(primero, []);
        File.WriteAllBytes(segundo, []);

        var imagenes = ImageFolderList.Resolve(segundo);

        Assert.That(imagenes, Is.EqualTo(new[] { primero, segundo }));
    }

    [Test]
    public void Resolve_DevuelveOrdenadoPorNombre()
    {
        var carpeta = Directory.CreateDirectory(Path.Combine(_root, "sesion")).FullName;
        var c = Path.Combine(carpeta, "game_3.png");
        var a = Path.Combine(carpeta, "game_1.png");
        var b = Path.Combine(carpeta, "game_2.png");
        File.WriteAllBytes(c, []);
        File.WriteAllBytes(a, []);
        File.WriteAllBytes(b, []);

        var imagenes = ImageFolderList.Resolve(c);

        Assert.That(imagenes, Is.EqualTo(new[] { a, b, c }));
    }

    [Test]
    public void Resolve_CarpetaSinPng_DevuelveVacio()
    {
        var carpeta = Directory.CreateDirectory(Path.Combine(_root, "vacia")).FullName;
        File.WriteAllText(Path.Combine(carpeta, "nota.txt"), "sin imagenes");

        var imagenes = ImageFolderList.Resolve(Path.Combine(carpeta, "nota.txt"));

        Assert.That(imagenes, Is.Empty);
    }

    [Test]
    public void Resolve_RutaSinCarpeta_DevuelveVacio()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ImageFolderList.Resolve(null), Is.Empty);
            Assert.That(ImageFolderList.Resolve("   "), Is.Empty);
            Assert.That(ImageFolderList.Resolve("game_suelto.png"), Is.Empty);
        });
    }

    [Test]
    public void FormImage_Toma_La_Lista_De_La_Carpeta_Del_Archivo_Elegido()
    {
        var source = File.ReadAllText(FormImagePath());

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("ImageFolderList.Resolve("),
                "El visor debe armar la lista desde la carpeta del archivo elegido.");
            Assert.That(source, Does.Not.Contain("Split(\"game\")"),
                "Cortar el path por \"game\" matchea el segmento \"games\" y devuelve otra carpeta.");
        });
    }

    private static string FormImagePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "OpenScrape.App", "Forms", "FormImage.cs");

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("No se encontró FormImage.cs desde el directorio de tests.");
    }
}
