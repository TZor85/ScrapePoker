using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Helpers;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio de captura de pantalla de la ventana de póker.
/// Extrae GetImageWhilePlaying, GetScaledRegion, SaveReferenceDimensionsToConfig y GetJsonValue de FrmMain.
/// NO gestiona el ciclo de vida de _handle ni de _session.
/// </summary>
public sealed class CaptureService : ICaptureService
{
    private readonly IGetWindowsScreenUseCase _useCase;
    private readonly ICoordinateScaler _coordinateScaler;
    private readonly ILogger<CaptureService> _logger;

    private int _lastCaptureWidth;
    private int _lastCaptureHeight;

    /// <inheritdoc />
    public IntPtr WindowHandle { get; set; }

    /// <inheritdoc />
    public Bitmap? CaptureWindow()
    {
        return CaptureWindow(WindowHandle);
    }

    public CaptureService(
        IGetWindowsScreenUseCase useCase,
        ICoordinateScaler coordinateScaler,
        ILogger<CaptureService> logger)
    {
        _useCase = useCase ?? throw new ArgumentNullException(nameof(useCase));
        _coordinateScaler = coordinateScaler ?? throw new ArgumentNullException(nameof(coordinateScaler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Bitmap? CaptureWindow(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            _logger.LogError("CaptureWindow: handle es IntPtr.Zero, no se puede capturar");
            return null;
        }

        try
        {
            var capturedImage = _useCase.Execute(handle);
            var capturedBitmap = capturedImage as Bitmap ?? new Bitmap(capturedImage);

            if (capturedBitmap.Width <= 1 || capturedBitmap.Height <= 1)
            {
                _logger.LogError("CaptureWindow: Captura inválida ({Width}x{Height})",
                    capturedBitmap.Width, capturedBitmap.Height);
                return null;
            }

            _lastCaptureWidth = capturedBitmap.Width;
            _lastCaptureHeight = capturedBitmap.Height;

            if (!_coordinateScaler.IsInitialized)
            {
                _coordinateScaler.Initialize(capturedBitmap.Width, capturedBitmap.Height);
                SaveReferenceDimensions(capturedBitmap.Width, capturedBitmap.Height);
            }

            return capturedBitmap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al capturar ventana");
            return null;
        }
    }

    /// <inheritdoc />
    public string? SaveFrame(Bitmap bitmap, string folderPath)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        try
        {
            Directory.CreateDirectory(folderPath);

            var path = BuildFramePath(folderPath);
            bitmap.Save(path, ImageFormat.Png);

            _logger.LogInformation("Frame guardado en {Path}", path);
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar frame en {FolderPath}", folderPath);
            return null;
        }
    }

    /// <inheritdoc />
    public DomainRegion GetScaledRegion(DomainRegion region)
    {
        if (_lastCaptureWidth == 0 || _lastCaptureHeight == 0)
            return region;

        var scaled = _coordinateScaler.ScaleRegion(
            region.PosX, region.PosY, region.Width, region.Height,
            _lastCaptureWidth, _lastCaptureHeight);

        return new DomainRegion(
            region.Category, region.Name,
            scaled.X, scaled.Y, scaled.Width, scaled.Height,
            region.IsHash, region.IsColor, region.IsBoard,
            region.Color, region.IsOnlyNumber,
            region.InactiveUmbral, region.Umbral);
    }

    /// <inheritdoc />
    public void SaveReferenceDimensions(int width, int height)
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
            {
                _logger.LogError("SaveReferenceDimensions: appsettings.json no encontrado");
                return;
            }

            var json = File.ReadAllText(configPath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms);
            using var reader = new StreamReader(ms);

            writer.Write("{");
            bool isFirst = true;

            foreach (var property in root.EnumerateObject())
            {
                if (!isFirst) writer.Write(",");
                isFirst = false;

                if (property.Name == "CaptureSettings")
                {
                    writer.Write("\"CaptureSettings\":{");
                    bool innerFirst = true;
                    foreach (var captureProp in property.Value.EnumerateObject())
                    {
                        if (!innerFirst) writer.Write(",");
                        innerFirst = false;

                        if (captureProp.Name == "ReferenceImageWidth")
                            writer.Write($"\"ReferenceImageWidth\":{width}");
                        else if (captureProp.Name == "ReferenceImageHeight")
                            writer.Write($"\"ReferenceImageHeight\":{height}");
                        else if (captureProp.Name == "IsReferenceSet")
                            writer.Write("\"IsReferenceSet\":true");
                        else
                            writer.Write($"\"{captureProp.Name}\":{FormatJsonValue(captureProp.Value)}");
                    }
                    writer.Write("}");
                }
                else
                {
                    writer.Write($"\"{property.Name}\":{FormatJsonValue(property.Value)}");
                }
            }

            writer.Write("}");
            writer.Flush();

            ms.Position = 0;
            File.WriteAllText(configPath, reader.ReadToEnd());

            _logger.LogInformation("Guardadas dimensiones de referencia: {Width}x{Height}", width, height);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar dimensiones de referencia");
        }
    }

    /// <inheritdoc />
    public string? GetJsonValue(string key, string section)
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
                return null;

            var json = File.ReadAllText(configPath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty(section, out var sectionElement) &&
                sectionElement.TryGetProperty(key, out var valueElement))
            {
                return valueElement.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => valueElement.GetString(),
                    System.Text.Json.JsonValueKind.Number => valueElement.GetRawText(),
                    System.Text.Json.JsonValueKind.True => "true",
                    System.Text.Json.JsonValueKind.False => "false",
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => valueElement.GetRawText()
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al leer valor JSON: [{Section}].{Key}", section, key);
            return null;
        }
    }

    /// <summary>
    /// Construye una ruta única para el frame dentro de la carpeta indicada.
    /// </summary>
    private static string BuildFramePath(string folderPath)
    {
        var stamp = DateTime.Now.Ticks;
        var path = Path.Combine(folderPath, $"game_{stamp}.png");
        var suffix = 1;

        while (File.Exists(path))
        {
            path = Path.Combine(folderPath, $"game_{stamp}_{suffix++}.png");
        }

        return path;
    }

    /// <summary>
    /// Formatea un JsonElement para escritura en JSON. Portado de FrmMain.GetJsonValue.
    /// </summary>
    private static string FormatJsonValue(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => $"\"{element.GetString()}\"",
            System.Text.Json.JsonValueKind.Number => element.GetRawText(),
            System.Text.Json.JsonValueKind.True => "true",
            System.Text.Json.JsonValueKind.False => "false",
            System.Text.Json.JsonValueKind.Null => "null",
            _ => element.GetRawText()
        };
    }
}
