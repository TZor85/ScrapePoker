using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del CaptureService extraído de FrmMain.
/// Cubre los escenarios de RF2: captura exitosa, handle inválido sin excepción,
/// escalado de región, y persistencia de dimensiones de referencia.
/// </summary>
[TestFixture]
public class CaptureServiceTests
{
    #region CaptureWindow Tests

    [Test]
    public void CaptureWindow_Con_Handle_Valido_Retorna_Bitmap()
    {
        // Arrange: crear un bitmap sintético de 200x100
        using var syntheticBitmap = new Bitmap(200, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(syntheticBitmap))
        {
            g.Clear(Color.Blue);
        }

        var fakeUseCase = new FakeGetWindowsScreenUseCase(syntheticBitmap);
        var scaler = new CoordinateScaler();
        scaler.Initialize(200, 100);
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        var logger = loggerFactory.CreateLogger<CaptureService>();

        var service = new CaptureService(fakeUseCase, scaler, logger);

        // Act
        var result = service.CaptureWindow((IntPtr)12345);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Debe retornar un Bitmap no nulo");
            Assert.That(result!.Width, Is.EqualTo(200), "Debe tener ancho 200");
            Assert.That(result.Height, Is.EqualTo(100), "Debe tener alto 100");
        });
    }

    [Test]
    public void CaptureWindow_Con_Handle_Zero_No_Lanza_Excepcion_Y_Retorna_Null()
    {
        // Arrange: la fake necesita un bitmap sintético (aunque nunca se usará porque handle es IntPtr.Zero)
        using var unusedBitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        var fakeUseCase = new FakeGetWindowsScreenUseCase(unusedBitmap);
        var scaler = new CoordinateScaler();
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        var logger = loggerFactory.CreateLogger<CaptureService>();

        var service = new CaptureService(fakeUseCase, scaler, logger);

        // Act & Assert: no debe lanzar excepción con IntPtr.Zero
        Bitmap? result = null;
        Assert.DoesNotThrow(() =>
        {
            result = service.CaptureWindow(IntPtr.Zero);
        });

        // Assert: debe retornar null
        Assert.That(result, Is.Null, "Debe retornar null para handle IntPtr.Zero");
    }

    [Test]
    public void CaptureWindow_Con_Handle_Valido_Inicializa_CoordinateScaler_Si_No_Esta_Inicializado()
    {
        // Arrange
        using var syntheticBitmap = new Bitmap(300, 150, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(syntheticBitmap))
        {
            g.Clear(Color.Green);
        }

        var fakeUseCase = new FakeGetWindowsScreenUseCase(syntheticBitmap);
        // CoordinateScaler NO inicializado
        var scaler = new CoordinateScaler();
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        var logger = loggerFactory.CreateLogger<CaptureService>();

        var service = new CaptureService(fakeUseCase, scaler, logger);

        // Act
        service.CaptureWindow((IntPtr)67890);

        // Assert: el CoordinateScaler debe quedar inicializado con las dimensiones de la captura
        Assert.Multiple(() =>
        {
            Assert.That(scaler.IsInitialized, Is.True, "Debe inicializar el CoordinateScaler");
            Assert.That(scaler.ReferenceWidth, Is.EqualTo(300));
            Assert.That(scaler.ReferenceHeight, Is.EqualTo(150));
        });
    }

    #endregion

    #region GetScaledRegion Tests

    [Test]
    public void GetScaledRegion_Con_Coordenadas_De_Referencia_Devuelve_Region_Escalada()
    {
        // Arrange: referencia 200x100, imagen capturada 400x200 (scale = 2x)
        var scaler = new CoordinateScaler();
        scaler.Initialize(200, 100);

        // Imagen de 400x200: el doble de la referencia → las coordenadas se escalan ~2x
        using var syntheticBitmap = new Bitmap(400, 200, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(syntheticBitmap))
        {
            g.Clear(Color.Red);
        }

        var fakeUseCase = new FakeGetWindowsScreenUseCase(syntheticBitmap);
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        var logger = loggerFactory.CreateLogger<CaptureService>();

        var service = new CaptureService(fakeUseCase, scaler, logger);

        // Capturar primero para que el servicio tenga dimensiones de la imagen actual (400x200)
        service.CaptureWindow((IntPtr)1);

        var region = new DomainRegion(
            Category: "Test", Name: "TestRegion",
            PosX: 10, PosY: 20, Width: 30, Height: 40,
            IsHash: null, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null);

        // Act
        var scaled = service.GetScaledRegion(region);

        // Assert: con scale ~2x, las coordenadas deben ser aproximadamente el doble
        Assert.Multiple(() =>
        {
            Assert.That(scaled, Is.Not.Null);
            Assert.That(scaled.PosX, Is.EqualTo(20), "PosX=10 * 2 = 20");
            Assert.That(scaled.PosY, Is.EqualTo(40), "PosY=20 * 2 = 40");
            Assert.That(scaled.Width, Is.EqualTo(60), "Width=30 * 2 = 60");
            Assert.That(scaled.Height, Is.EqualTo(80), "Height=40 * 2 = 80");
        });
    }

    [Test]
    public void GetScaledRegion_Sin_Imagen_Devuelve_Coordenadas_Originales()
    {
        // Arrange: CoordinateScaler inicializado pero sin captura previa
        var scaler = new CoordinateScaler();
        scaler.Initialize(200, 100);

        using var syntheticBitmap = new Bitmap(200, 100, PixelFormat.Format32bppArgb);
        var fakeUseCase = new FakeGetWindowsScreenUseCase(syntheticBitmap);
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        var logger = loggerFactory.CreateLogger<CaptureService>();

        // Servicio sin captura previa
        var service = new CaptureService(fakeUseCase, scaler, logger);

        var region = new DomainRegion(
            Category: "Test", Name: "TestRegion",
            PosX: 50, PosY: 60, Width: 70, Height: 80,
            IsHash: null, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null);

        // Act
        var result = service.GetScaledRegion(region);

        // Assert: sin imagen actual, devuelve las coordenadas originales (mismo comportamiento que FrmMain legacy)
        Assert.Multiple(() =>
        {
            Assert.That(result.PosX, Is.EqualTo(50));
            Assert.That(result.PosY, Is.EqualTo(60));
            Assert.That(result.Width, Is.EqualTo(70));
            Assert.That(result.Height, Is.EqualTo(80));
        });
    }

    #endregion

    #region SaveReferenceDimensions Tests

    [Test]
    public void SaveReferenceDimensions_No_Lanza_Excepcion()
    {
        // Arrange
        using var unusedBitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        var fakeUseCase = new FakeGetWindowsScreenUseCase(unusedBitmap);
        var scaler = new CoordinateScaler();
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        var logger = loggerFactory.CreateLogger<CaptureService>();

        var service = new CaptureService(fakeUseCase, scaler, logger);

        // Act & Assert: no debe lanzar excepción
        Assert.DoesNotThrow(() =>
        {
            service.SaveReferenceDimensions(800, 600);
        });
    }

    #endregion

    #region SaveFrame Tests

    [Test]
    public void SaveFrame_Guarda_Png_En_La_Carpeta_Indicada()
    {
        using var bitmap = new Bitmap(20, 10, PixelFormat.Format32bppArgb);
        var service = CreateService(bitmap);
        var folder = CreateTemporaryFolder();

        try
        {
            var path = service.SaveFrame(bitmap, folder);

            Assert.Multiple(() =>
            {
                Assert.That(path, Is.Not.Null);
                Assert.That(File.Exists(path), Is.True, "El archivo debe existir en disco");
                Assert.That(Path.GetExtension(path), Is.EqualTo(".png"));
                Assert.That(Path.GetDirectoryName(path), Is.EqualTo(folder));
            });

            using var saved = Image.FromFile(path!);
            Assert.Multiple(() =>
            {
                Assert.That(saved.Width, Is.EqualTo(20));
                Assert.That(saved.Height, Is.EqualTo(10));
            });
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public void SaveFrame_Crea_La_Carpeta_Si_No_Existe()
    {
        using var bitmap = new Bitmap(5, 5, PixelFormat.Format32bppArgb);
        var service = CreateService(bitmap);
        var root = CreateTemporaryFolder();
        var folder = Path.Combine(root, "sub", "sesion");

        try
        {
            var path = service.SaveFrame(bitmap, folder);

            Assert.That(File.Exists(path), Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void SaveFrame_Genera_Nombres_Unicos_En_Guardados_Consecutivos()
    {
        using var bitmap = new Bitmap(5, 5, PixelFormat.Format32bppArgb);
        var service = CreateService(bitmap);
        var folder = CreateTemporaryFolder();

        try
        {
            var first = service.SaveFrame(bitmap, folder);
            var second = service.SaveFrame(bitmap, folder);

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.Not.EqualTo(second));
                Assert.That(File.Exists(first), Is.True);
                Assert.That(File.Exists(second), Is.True);
            });
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public void SaveFrame_Con_Bitmap_Nulo_Lanza_ArgumentNullException()
    {
        using var unusedBitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        var service = CreateService(unusedBitmap);

        Assert.Throws<ArgumentNullException>(
            () => service.SaveFrame(null!, CreateTemporaryFolder()));
    }

    #endregion

    #region Helpers

    private static CaptureService CreateService(Bitmap bitmap)
    {
        var fakeUseCase = new FakeGetWindowsScreenUseCase(bitmap);
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        return new CaptureService(fakeUseCase, new CoordinateScaler(), loggerFactory.CreateLogger<CaptureService>());
    }

    private static string CreateTemporaryFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"capture-service-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>
    /// Fake para IGetWindowsScreenUseCase que retorna un bitmap sintético.
    /// </summary>
    private sealed class FakeGetWindowsScreenUseCase : IGetWindowsScreenUseCase
    {
        private readonly Bitmap _bitmap;

        public FakeGetWindowsScreenUseCase(Bitmap bitmap)
        {
            _bitmap = bitmap;
        }

        public Image Execute(IntPtr handle)
        {
            // Retornar una copia para simular comportamiento real
            return new Bitmap(_bitmap!);
        }
    }

    #endregion
}
