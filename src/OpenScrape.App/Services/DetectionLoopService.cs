using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Aplication.UseCases;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio de loop de detección de píxeles. Reemplaza el BackgroundWorker de FrmMain
/// por un Task.Run con notificación por eventos, preservando el algoritmo de detección
/// original (LockBits, muestreo en cruz ±2px, tolerancia de color).
/// </summary>
public class DetectionLoopService : IDetectionLoopService
{
    private readonly GetWindowsScreenUseCase _useCase;
    private readonly RegionLookupCache _regionLookupCache;
    private readonly DetectionLoggerService _detectionLoggerService;
    private readonly ICoordinateScaler _coordinateScaler;
    private readonly ILogger<DetectionLoopService> _logger;

    private volatile bool _executeCapture;
    private volatile bool _isRunning;
    private Task? _loopTask;
    private CancellationTokenSource? _internalCts;
    private Func<bool>? _isOverlayVisible;
    private IntPtr _handle;

    private int _totalChecks;
    private int _detections;
    private int _errors;

    // Eventos
    public event EventHandler<HeroTurnDetectedEventArgs>? HeroTurnDetected;
    public event EventHandler<FlopDetectedEventArgs>? FlopDetected;
    public event EventHandler? DetectionStopped;
    public event EventHandler? IterationCompleted;

    public bool IsRunning => _isRunning;
    public int TotalChecks => _totalChecks;
    public int Detections => _detections;
    public int Errors => _errors;

    public DetectionLoopService(
        GetWindowsScreenUseCase useCase,
        RegionLookupCache regionLookupCache,
        DetectionLoggerService detectionLoggerService,
        ICoordinateScaler coordinateScaler,
        ILogger<DetectionLoopService> logger)
    {
        _useCase = useCase ?? throw new ArgumentNullException(nameof(useCase));
        _regionLookupCache = regionLookupCache ?? throw new ArgumentNullException(nameof(regionLookupCache));
        _detectionLoggerService = detectionLoggerService ?? throw new ArgumentNullException(nameof(detectionLoggerService));
        _coordinateScaler = coordinateScaler ?? throw new ArgumentNullException(nameof(coordinateScaler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Inicia el loop de detección en background.
    /// </summary>
    public Task StartAsync(IntPtr windowHandle, Func<bool> isOverlayVisible, CancellationToken cancellationToken)
    {
        if (_isRunning)
            return Task.CompletedTask;

        _isRunning = true;
        _isOverlayVisible = isOverlayVisible ?? throw new ArgumentNullException(nameof(isOverlayVisible));
        _handle = windowHandle;
        _executeCapture = false;
        _totalChecks = 0;
        _detections = 0;
        _errors = 0;

        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _loopTask = Task.Run(() => DetectionLoop(_handle, _internalCts.Token), _internalCts.Token);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Detiene el loop de detección y espera su finalización.
    /// </summary>
    public async Task StopAsync()
    {
        _isRunning = false;
        try
        {
            _internalCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Ya estaba cancelado
        }

        if (_loopTask != null)
        {
            try
            {
                await _loopTask;
            }
            catch (OperationCanceledException)
            {
                // Esperado al cancelar
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error esperando finalización del loop de detección");
            }
        }

        _internalCts?.Dispose();
        _internalCts = null;
    }

    /// <summary>
    /// Loop principal de detección. Replica exactamente la lógica de
    /// FrmMain.BackgroundWorker1_DoWork, reemplazando this.Invoke() por eventos.
    /// </summary>
    private async Task DetectionLoop(IntPtr handle, CancellationToken ct)
    {
        try
        {
            var detectionStats = (StartTime: DateTime.Now, TotalChecks: 0, Detections: 0, Errors: 0);
            var lastLoggedColor = Color.Empty;
            var colorChangeCount = 0;

            _detectionLoggerService.LogDetectionError("DetectionLoopService iniciado - Monitoreo de detección activo");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    detectionStats.TotalChecks++;
                    _totalChecks = detectionStats.TotalChecks;

                    // Validación de overlay
                    if (_isOverlayVisible != null && !_isOverlayVisible())
                    {
                        _detectionLoggerService.LogDetectionError("Overlay no visible - Cancelando DetectionLoopService");
                        _isRunning = false;
                        return;
                    }

                    if (handle == IntPtr.Zero)
                    {
                        _detectionLoggerService.LogDetectionError("Handle es IntPtr.Zero - no se puede capturar");
                        await Task.Delay(500, ct);
                        ct.ThrowIfCancellationRequested();
                        continue;
                    }

                    using var img = _useCase.Execute(handle);

                    // Verificar cancelación tras la captura (operación bloqueante)
                    ct.ThrowIfCancellationRequested();

                    if (img == null || img.Width <= 1 || img.Height <= 1)
                    {
                        _detectionLoggerService.LogDetectionError($"Captura inválida: {img?.Width}x{img?.Height}");
                        await Task.Delay(500, ct);
                        ct.ThrowIfCancellationRequested();
                        continue;
                    }

                    // Validación de regiones
                    var regionAction = _regionLookupCache.GetRegion("User", "uAction");
                    var flop = _regionLookupCache.GetRegion("Table", "isFlop");

                    if (regionAction == null || flop == null)
                    {
                        if (detectionStats.TotalChecks % 100 == 0)
                        {
                            _detectionLoggerService.LogDetectionError(
                                $"Regiones no encontradas - uAction: {regionAction != null}, isFlop: {flop != null}");
                        }
                        await Task.Delay(100, ct);
                        ct.ThrowIfCancellationRequested();
                        continue;
                    }

                    using var bitmap = new Bitmap(img);

                    // Detección mejorada con múltiples píxeles y tolerancia
                    var detectionResult = PerformEnhancedDetection(bitmap, regionAction, flop);

                    // Log cambios de color significativos
                    if (!detectionResult.ActionColor.Equals(lastLoggedColor))
                    {
                        colorChangeCount++;
                        if (colorChangeCount % 10 == 0 || Math.Abs(detectionResult.ActionColor.B - lastLoggedColor.B) > 5)
                        {
                            _detectionLoggerService.LogColorDetection(
                                new Point(regionAction.PosX, regionAction.PosY),
                                detectionResult.ActionColor,
                                detectionResult.ShouldCapture,
                                "uAction"
                            );
                            lastLoggedColor = detectionResult.ActionColor;
                        }
                    }

                    // Detección de flop
                    if (detectionResult.ShouldCaptureFlop)
                    {
                        _detectionLoggerService.LogTurnDetected(
                            new Point(regionAction.PosX, regionAction.PosY),
                            detectionResult.ActionColor,
                            "FlopDetected"
                        );

                        FlopDetected?.Invoke(this, new FlopDetectedEventArgs(
                            detectionResult.FlopColor,
                            DateTimeOffset.Now));
                    }

                    // Detección de turno del héroe
                    if (detectionResult.ShouldCapture)
                    {
                        detectionStats.Detections++;
                        _detections = detectionStats.Detections;

                        _detectionLoggerService.LogTurnDetected(
                            new Point(regionAction.PosX, regionAction.PosY),
                            detectionResult.ActionColor,
                            "HeroTurnDetected"
                        );

                        // Guardar screenshot de debug si está habilitado
                        if (detectionStats.Detections % 5 == 0)
                        {
                            _detectionLoggerService.SaveDebugScreenshot(
                                img,
                                new Point(regionAction.PosX, regionAction.PosY),
                                detectionResult.ActionColor,
                                "turn_detected"
                            );
                        }

                        HeroTurnDetected?.Invoke(this, new HeroTurnDetectedEventArgs(
                            detectionResult.ActionColor,
                            GameState.WaitingForHand, // El estado real lo gestiona FrmMain
                            DateTimeOffset.Now));

                        // Prevenir re-detección hasta que el color salga del rango
                        _executeCapture = true;
                    }

                    // Reset del flag de ejecución
                    if (!detectionResult.IsActionColorInRange)
                        _executeCapture = false;

                    // Log estadísticas periódicas
                    if (detectionStats.TotalChecks % 1000 == 0)
                    {
                        var sessionDuration = DateTime.Now - detectionStats.StartTime;
                        _detectionLoggerService.LogDetectionStatistics(
                            detectionStats.TotalChecks,
                            detectionStats.Detections,
                            detectionStats.Errors,
                            sessionDuration
                        );
                    }

                    // Emitir evento para tracking de ventana (reemplaza btnWindow_Click en el loop)
                    IterationCompleted?.Invoke(this, EventArgs.Empty);

                    // Delay adaptativo basado en la actividad
                    var delay = detectionResult.ShouldCapture ? 200 : 100;
                    await Task.Delay(delay, ct);
                    ct.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    // Cancelación solicitada, salir limpiamente
                    return;
                }
                catch (Exception loopEx)
                {
                    detectionStats.Errors++;
                    _errors = detectionStats.Errors;
                    _detectionLoggerService.LogDetectionError($"Error en bucle de detección: {loopEx.Message}", loopEx);

                    // Delay más largo en caso de error para evitar spam
                    await Task.Delay(500, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelación del token externo
        }
        catch (Exception ex)
        {
            _detectionLoggerService.LogDetectionError($"Error crítico en DetectionLoopService: {ex.Message}", ex);
            _logger.LogError(ex, "Error en DetectionLoopService");
        }
        finally
        {
            _isRunning = false;
            DetectionStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Realiza detección mejorada con múltiples píxeles y tolerancia de color.
    /// Extraído de FrmMain.PerformEnhancedDetection — paridad funcional estricta.
    /// </summary>
    public DetectionResult PerformEnhancedDetection(Bitmap bitmap, DomainRegion regionAction, DomainRegion flop)
    {
        try
        {
            // Escalar coordenadas de las regiones
            var (sx, sy, sw, sh) = GetScaledRegion(bitmap, regionAction);
            var (fx, fy, fw, fh) = GetScaledRegion(bitmap, flop);

            int w = bitmap.Width;
            int h = bitmap.Height;

            // Acceso directo al buffer de píxeles (evita marshaling de GetPixel)
            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            try
            {
                int bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                int stride = bmpData.Stride;
                nint scan0 = bmpData.Scan0;

                // Leer color principal de acción y flop
                var (actionR, actionG, actionB) = ReadPixelFromBuffer(scan0, stride, sx, sy, bpp);
                var (flopR, flopG, flopB) = ReadPixelFromBuffer(scan0, stride, fx, fy, bpp);

                Color primaryActionColor = Color.FromArgb(actionR, actionG, actionB);
                Color flopColor = Color.FromArgb(flopR, flopG, flopB);

                // Muestrear píxeles en patrón de cruz (±2 px) — suma manual sin LINQ
                int sumR = actionR, sumG = actionG, sumB = actionB;
                int count = 1;

                ReadOnlySpan<(int dx, int dy)> offsets =
                [
                    (-2, 0), (2, 0), (0, -2), (0, 2),
                    (-1, -1), (1, 1), (-1, 1), (1, -1)
                ];

                foreach (var (dx, dy) in offsets)
                {
                    int x = sx + dx;
                    int y = sy + dy;

                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        var (r, g, b) = ReadPixelFromBuffer(scan0, stride, x, y, bpp);
                        sumR += r;
                        sumG += g;
                        sumB += b;
                        count++;
                    }
                }

                double avgB = (double)sumB / count;
                double avgR = (double)sumR / count;
                double avgG = (double)sumG / count;

                // Detección con rango de tolerancia en lugar de valor exacto
                const int TARGET_B = 24;
                const int TOLERANCE = 3; // Tolerancia de ±3 para el valor B

                bool isActionColorInRange = Math.Abs(avgB - TARGET_B) <= TOLERANCE;
                bool shouldCapture = isActionColorInRange && !_executeCapture;

                // Detección de flop mejorada
                const int FLOP_TARGET_B = 255;
                const int FLOP_TOLERANCE = 10;
                bool isFlopVisible = Math.Abs(flopB - FLOP_TARGET_B) <= FLOP_TOLERANCE;
                bool shouldCaptureFlop = shouldCapture && isFlopVisible;

                return new DetectionResult
                {
                    ActionColor = primaryActionColor,
                    FlopColor = flopColor,
                    AverageActionB = avgB,
                    SampleCount = count,
                    IsActionColorInRange = isActionColorInRange,
                    ShouldCapture = shouldCapture,
                    ShouldCaptureFlop = shouldCaptureFlop,
                    IsFlopVisible = isFlopVisible
                };
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }
        catch (Exception ex)
        {
            _detectionLoggerService.LogDetectionError($"Error en detección mejorada: {ex.Message}", ex);

            // Fallback a detección simple (GetPixel como último recurso)
            var (sx, sy, _, _) = GetScaledRegion(bitmap, regionAction);
            var (fx, fy, _, _) = GetScaledRegion(bitmap, flop);
            Color actionColor = bitmap.GetPixel(sx, sy);
            Color flopColor = bitmap.GetPixel(fx, fy);

            return new DetectionResult
            {
                ActionColor = actionColor,
                FlopColor = flopColor,
                AverageActionB = actionColor.B,
                SampleCount = 1,
                IsActionColorInRange = actionColor.B == 24,
                ShouldCapture = actionColor.B == 24 && !_executeCapture,
                ShouldCaptureFlop = actionColor.B == 24 && !_executeCapture && flopColor.B == 255,
                IsFlopVisible = flopColor.B == 255
            };
        }
    }

    /// <summary>
    /// Lee un píxel directamente del buffer de LockBits (soporta 24bpp y 32bpp).
    /// Extraído de FrmMain.ReadPixelFromBuffer — paridad funcional estricta.
    /// </summary>
    public static (byte R, byte G, byte B) ReadPixelFromBuffer(nint scan0, int stride, int x, int y, int bpp)
    {
        int offset = y * stride + x * bpp;
        byte b = System.Runtime.InteropServices.Marshal.ReadByte(scan0, offset);
        byte g = System.Runtime.InteropServices.Marshal.ReadByte(scan0, offset + 1);
        byte r = System.Runtime.InteropServices.Marshal.ReadByte(scan0, offset + 2);
        return (r, g, b);
    }

    /// <summary>
    /// Escala las coordenadas de una región según el tamaño real de la imagen.
    /// Equivalente a FrmMain.GetScaledRegion pero usando el bitmap capturado.
    /// </summary>
    private (int X, int Y, int Width, int Height) GetScaledRegion(Bitmap bitmap, DomainRegion region)
    {
        if (!_coordinateScaler.IsInitialized)
            return (region.PosX, region.PosY, region.Width, region.Height);

        return _coordinateScaler.ScaleRegion(
            region.PosX, region.PosY, region.Width, region.Height,
            bitmap.Width, bitmap.Height);
    }

    /// <summary>
    /// Solo para tests: fuerza el estado interno de _executeCapture.
    /// </summary>
    public void SetExecuteCaptureForTest(bool value)
    {
        _executeCapture = value;
    }
}

/// <summary>
/// Resultado de la detección mejorada de píxeles.
/// Extraído del record privado FrmMain.DetectionResult.
/// </summary>
public class DetectionResult
{
    public Color ActionColor { get; set; }
    public Color FlopColor { get; set; }
    public double AverageActionB { get; set; }
    public int SampleCount { get; set; }
    public bool IsActionColorInRange { get; set; }
    public bool ShouldCapture { get; set; }
    public bool ShouldCaptureFlop { get; set; }
    public bool IsFlopVisible { get; set; }
}
