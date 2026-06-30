using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del DetectionLoopService extraído de FrmMain.BackgroundWorker1_DoWork.
/// Cubre los escenarios de RF1: detección de turno del héroe, overlay no visible,
/// captura inválida, cancelación limpia, y métricas.
/// </summary>
[TestFixture]
public class DetectionLoopServiceTests
{
    #region ReadPixelFromBuffer Tests

    [Test]
    public void ReadPixelFromBuffer_Lee_Color_RGB_Correctamente_32bpp()
    {
        // Arrange: crear un bitmap de 2x1 píxeles con color conocido (Red=10, Green=20, Blue=30)
        using var bitmap = new Bitmap(2, 1, PixelFormat.Format32bppArgb);
        bitmap.SetPixel(0, 0, Color.FromArgb(10, 20, 30));
        bitmap.SetPixel(1, 0, Color.FromArgb(40, 50, 60));

        var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, 2, 1),
            ImageLockMode.ReadOnly,
            bitmap.PixelFormat);

        try
        {
            int bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int stride = bmpData.Stride;
            nint scan0 = bmpData.Scan0;

            // Act
            var (r1, g1, b1) = DetectionLoopService.ReadPixelFromBuffer(scan0, stride, 0, 0, bpp);
            var (r2, g2, b2) = DetectionLoopService.ReadPixelFromBuffer(scan0, stride, 1, 0, bpp);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(r1, Is.EqualTo(10), "R del pixel (0,0)");
                Assert.That(g1, Is.EqualTo(20), "G del pixel (0,0)");
                Assert.That(b1, Is.EqualTo(30), "B del pixel (0,0)");

                Assert.That(r2, Is.EqualTo(40), "R del pixel (1,0)");
                Assert.That(g2, Is.EqualTo(50), "G del pixel (1,0)");
                Assert.That(b2, Is.EqualTo(60), "B del pixel (1,0)");
            });
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }

    [Test]
    public void ReadPixelFromBuffer_Lee_Color_RGB_Correctamente_24bpp()
    {
        // Arrange: bitmap 24bpp (sin alpha)
        using var bitmap = new Bitmap(1, 1, PixelFormat.Format24bppRgb);
        bitmap.SetPixel(0, 0, Color.FromArgb(100, 150, 200));

        var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, 1, 1),
            ImageLockMode.ReadOnly,
            bitmap.PixelFormat);

        try
        {
            int bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            nint scan0 = bmpData.Scan0;
            int stride = bmpData.Stride;

            // Act
            var (r, g, b) = DetectionLoopService.ReadPixelFromBuffer(scan0, stride, 0, 0, bpp);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(r, Is.EqualTo(100));
                Assert.That(g, Is.EqualTo(150));
                Assert.That(b, Is.EqualTo(200));
            });
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }

    #endregion

    #region PerformEnhancedDetection Tests

    [Test]
    public void PerformEnhancedDetection_Detecta_Turno_Heroe_Cuando_Color_Coincide()
    {
        // Arrange: bitmap 10x10 con pixel en (5,5) de color B=24, G y R arbitrarios
        using var bitmap = new Bitmap(10, 10, PixelFormat.Format24bppRgb);
        // Rellenar todo de blanco
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.White);
        }
        // Pixel de acción: B=24 (color esperado de uAction)
        bitmap.SetPixel(5, 5, Color.FromArgb(128, 0, 24));
        // Píxeles adyacentes: variación controlada para el muestreo en cruz
        bitmap.SetPixel(3, 5, Color.FromArgb(128, 0, 24));  // -2, 0
        bitmap.SetPixel(7, 5, Color.FromArgb(128, 0, 24));  // +2, 0
        bitmap.SetPixel(5, 3, Color.FromArgb(128, 0, 24));  // 0, -2
        bitmap.SetPixel(5, 7, Color.FromArgb(128, 0, 24));  // 0, +2
        bitmap.SetPixel(4, 4, Color.FromArgb(128, 0, 24));  // -1, -1
        bitmap.SetPixel(6, 6, Color.FromArgb(128, 0, 24));  // +1, +1
        bitmap.SetPixel(4, 6, Color.FromArgb(128, 0, 24));  // -1, +1
        bitmap.SetPixel(6, 4, Color.FromArgb(128, 0, 24));  // +1, -1

        // Pixel de flop: B=100 (no es blanco, no detecta flop)
        bitmap.SetPixel(8, 8, Color.FromArgb(100, 100, 100));

        var regionCache = CreateRegionCache(
            ("User", "uAction", 5, 5, 3, 3),
            ("Table", "isFlop", 8, 8, 3, 3));

        var service = CreateService(regionCache);

        // Act
        var result = service.PerformEnhancedDetection(bitmap,
            regionCache.GetRegion("User", "uAction")!,
            regionCache.GetRegion("Table", "isFlop")!);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsActionColorInRange, Is.True,
                "El color B~24 debe estar en rango");
            Assert.That(result.ShouldCapture, Is.True,
                "Debe capturar: color en rango y _executeCapture=false");
            Assert.That(result.IsFlopVisible, Is.False,
                "Flop pixel B=100 no está en rango de flop (target B=255 ±10)");
            Assert.That(result.SampleCount, Is.EqualTo(9),
                "9 muestras (1 central + 8 en cruz ±2)");
        });
    }

    [Test]
    public void PerformEnhancedDetection_No_Detecta_Cuando_Color_Fuera_De_Rango()
    {
        // Arrange: bitmap con color de acción B=100 (fuera del rango 24±3)
        using var bitmap = new Bitmap(10, 10, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.White);
        }
        bitmap.SetPixel(5, 5, Color.FromArgb(128, 0, 100));

        var regionCache = CreateRegionCache(
            ("User", "uAction", 5, 5, 3, 3),
            ("Table", "isFlop", 8, 8, 3, 3));

        var service = CreateService(regionCache);

        // Act
        var result = service.PerformEnhancedDetection(bitmap,
            regionCache.GetRegion("User", "uAction")!,
            regionCache.GetRegion("Table", "isFlop")!);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsActionColorInRange, Is.False);
            Assert.That(result.ShouldCapture, Is.False);
        });
    }

    [Test]
    public void PerformEnhancedDetection_No_ReDetecta_Cuando_ExecuteCapture_True()
    {
        // Arrange: bitmap con color B=24 pero _executeCapture ya es true
        using var bitmap = new Bitmap(10, 10, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.White);
        }
        // Setear TODOS los píxeles del patrón de cruz para que el promedio B ~ 24
        bitmap.SetPixel(5, 5, Color.FromArgb(128, 0, 24));
        bitmap.SetPixel(3, 5, Color.FromArgb(128, 0, 24));  // -2, 0
        bitmap.SetPixel(7, 5, Color.FromArgb(128, 0, 24));  // +2, 0
        bitmap.SetPixel(5, 3, Color.FromArgb(128, 0, 24));  // 0, -2
        bitmap.SetPixel(5, 7, Color.FromArgb(128, 0, 24));  // 0, +2
        bitmap.SetPixel(4, 4, Color.FromArgb(128, 0, 24));  // -1, -1
        bitmap.SetPixel(6, 6, Color.FromArgb(128, 0, 24));  // +1, +1
        bitmap.SetPixel(4, 6, Color.FromArgb(128, 0, 24));  // -1, +1
        bitmap.SetPixel(6, 4, Color.FromArgb(128, 0, 24));  // +1, -1

        var regionCache = CreateRegionCache(
            ("User", "uAction", 5, 5, 3, 3),
            ("Table", "isFlop", 8, 8, 3, 3));

        var service = CreateService(regionCache);
        service.SetExecuteCaptureForTest(true); // Simula que ya se capturó

        // Act
        var result = service.PerformEnhancedDetection(bitmap,
            regionCache.GetRegion("User", "uAction")!,
            regionCache.GetRegion("Table", "isFlop")!);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsActionColorInRange, Is.True,
                "Color sigue en rango aunque no deba capturar");
            Assert.That(result.ShouldCapture, Is.False,
                "No debe capturar porque _executeCapture=true");
        });
    }

    #endregion

    #region Service Lifecycle Tests

    [Test]
    public async Task StartAsync_Establece_IsRunning_True()
    {
        var service = CreateService();
        var cts = new CancellationTokenSource();

        await service.StartAsync((IntPtr)1, () => true, cts.Token);

        Assert.That(service.IsRunning, Is.True);

        await service.StopAsync();
        cts.Dispose();
    }

    [Test]
    public async Task StopAsync_Establece_IsRunning_False_Y_Emite_DetectionStopped()
    {
        var service = CreateService();
        var cts = new CancellationTokenSource();
        bool stoppedEventFired = false;
        service.DetectionStopped += (_, _) => stoppedEventFired = true;

        await service.StartAsync((IntPtr)1, () => true, cts.Token);

        // Pequeña pausa para asegurar que el Task.Run haya comenzado a ejecutar
        await Task.Delay(50);

        await service.StopAsync();

        Assert.Multiple(() =>
        {
            Assert.That(service.IsRunning, Is.False);
            Assert.That(stoppedEventFired, Is.True);
        });

        cts.Dispose();
    }

    [Test]
    public async Task StartAsync_Con_Handle_Zero_No_Lanza_Excepcion()
    {
        var service = CreateService();
        var cts = new CancellationTokenSource();

        // El servicio debe manejar IntPtr.Zero sin lanzar excepción
        Assert.DoesNotThrowAsync(async () =>
        {
            await service.StartAsync(IntPtr.Zero, () => true, cts.Token);
            // Dar tiempo para que el loop detecte el handle inválido
            await Task.Delay(100);
        });

        await service.StopAsync();
        cts.Dispose();
    }

    [Test]
    public async Task Loop_Se_Detiene_Cuando_Overlay_No_Visible()
    {
        var service = CreateService();
        var cts = new CancellationTokenSource();
        bool stoppedEventFired = false;
        service.DetectionStopped += (_, _) => stoppedEventFired = true;

        // Overlay no visible desde el inicio
        await service.StartAsync((IntPtr)1, () => false, cts.Token);

        // Esperar que el loop se detenga solo (máximo 5s)
        var timeout = Task.Delay(5000);
        while (!stoppedEventFired && !timeout.IsCompleted)
        {
            await Task.Delay(100);
        }

        Assert.That(stoppedEventFired, Is.True,
            "El loop debe detenerse cuando el overlay no está visible");

        await service.StopAsync();
        cts.Dispose();
    }

    [Test]
    public async Task Loop_Se_Cancela_Con_CancellationToken()
    {
        var service = CreateService();
        var cts = new CancellationTokenSource();
        bool stoppedEventFired = false;
        service.DetectionStopped += (_, _) => stoppedEventFired = true;

        await service.StartAsync((IntPtr)1, () => true, cts.Token);

        // Pequeña pausa para asegurar que el loop haya comenzado antes de cancelar
        await Task.Delay(50);

        // Cancelar externamente
        cts.Cancel();

        // Esperar que el loop se detenga
        var timeout = Task.Delay(5000);
        while (!stoppedEventFired && !timeout.IsCompleted)
        {
            await Task.Delay(100);
        }

        Assert.That(stoppedEventFired, Is.True,
            "El loop debe detenerse al cancelar el CancellationToken");

        await service.StopAsync();
        cts.Dispose();
    }

    #endregion

    #region DetectionResult Tests

    [Test]
    public void DetectionResult_Registra_Valores_Correctos()
    {
        var result = new DetectionResult
        {
            ActionColor = Color.FromArgb(10, 20, 24),
            FlopColor = Color.White,
            AverageActionB = 24.0,
            SampleCount = 9,
            IsActionColorInRange = true,
            ShouldCapture = true,
            ShouldCaptureFlop = false,
            IsFlopVisible = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.ActionColor.B, Is.EqualTo(24));
            Assert.That(result.AverageActionB, Is.EqualTo(24.0));
            Assert.That(result.SampleCount, Is.EqualTo(9));
            Assert.That(result.IsActionColorInRange, Is.True);
            Assert.That(result.ShouldCapture, Is.True);
            Assert.That(result.ShouldCaptureFlop, Is.False);
            Assert.That(result.IsFlopVisible, Is.True);
        });
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Crea un RegionLookupCache con las regiones especificadas.
    /// </summary>
    private static RegionLookupCache CreateRegionCache(params (string MapId, string Name, int X, int Y, int W, int H)[] regions)
    {
        var cache = new RegionLookupCache();
        var maps = new List<RegionTableMap>();

        var grouped = regions.GroupBy(r => r.MapId);
        foreach (var group in grouped)
        {
            var map = new RegionTableMap
            {
                Id = group.Key,
                Regions = group.Select(r =>
                    new DomainRegion(
                        Category: group.Key,
                        Name: r.Name,
                        PosX: r.X,
                        PosY: r.Y,
                        Width: r.W,
                        Height: r.H,
                        IsHash: null,
                        IsColor: null,
                        IsBoard: null,
                        Color: null,
                        IsOnlyNumber: null,
                        InactiveUmbral: null,
                        Umbral: null
                    )).ToList()
            };
            maps.Add(map);
        }

        cache.Initialize(maps);
        return cache;
    }

    /// <summary>
    /// Crea un DetectionLoopService con dependencias reales mínimas.
    /// El screen capture usa un fake que retorna null (para tests de lifecycle).
    /// </summary>
    private static DetectionLoopService CreateService(RegionLookupCache? regionCache = null)
    {
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        var logger = loggerFactory.CreateLogger<DetectionLoopService>();

        var useCase = new GetWindowsScreenUseCase();
        var regionLookupCache = regionCache ?? new RegionLookupCache();
        var detectionLogger = new DetectionLoggerService();
        var coordinateScaler = new CoordinateScaler();

        return new DetectionLoopService(
            useCase,
            regionLookupCache,
            detectionLogger,
            coordinateScaler,
            logger);
    }

    #endregion
}
