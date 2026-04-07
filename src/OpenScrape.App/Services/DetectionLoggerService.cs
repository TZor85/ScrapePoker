using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio de logging específico para la detección de turnos del Hero
/// </summary>
public class DetectionLoggerService
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly string _screenshotDirectory;
    private readonly object _lockObject = new();

    public DetectionLoggerService()
    {
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Detection");
        _screenshotDirectory = Path.Combine(_logDirectory, "Screenshots");
        _logFilePath = Path.Combine(_logDirectory, $"detection_{DateTime.Now:yyyyMMdd}.log");

        // Crear directorios si no existen
        Directory.CreateDirectory(_logDirectory);
        Directory.CreateDirectory(_screenshotDirectory);
    }

    /// <summary>
    /// Registra información de detección de color
    /// </summary>
    public void LogColorDetection(Point coordinates, Color detectedColor, bool isExpectedColor, string regionName = "uAction")
    {
        var logEntry = new
        {
            Timestamp = DateTime.Now,
            Type = "ColorDetection",
            Region = regionName,
            Coordinates = new { X = coordinates.X, Y = coordinates.Y },
            Color = new
            {
                R = detectedColor.R,
                G = detectedColor.G,
                B = detectedColor.B,
                Hex = $"#{detectedColor.R:X2}{detectedColor.G:X2}{detectedColor.B:X2}"
            },
            IsExpectedColor = isExpectedColor,
            ExpectedValue = "B=24"
        };

        WriteLogEntry(logEntry);
    }

    /// <summary>
    /// Registra cuando se detecta un turno del Hero
    /// </summary>
    public void LogTurnDetected(Point coordinates, Color detectedColor, string gameState = "Unknown")
    {
        var logEntry = new
        {
            Timestamp = DateTime.Now,
            Type = "TurnDetected",
            Coordinates = new { X = coordinates.X, Y = coordinates.Y },
            Color = new
            {
                R = detectedColor.R,
                G = detectedColor.G,
                B = detectedColor.B,
                Hex = $"#{detectedColor.R:X2}{detectedColor.G:X2}{detectedColor.B:X2}"
            },
            GameState = gameState,
            Message = "Hero turn detected - triggering capture"
        };

        WriteLogEntry(logEntry);
    }

    /// <summary>
    /// Registra errores en la detección
    /// </summary>
    public void LogDetectionError(string error, Exception? exception = null)
    {
        var logEntry = new
        {
            Timestamp = DateTime.Now,
            Type = "DetectionError",
            Error = error,
            Exception = exception?.ToString(),
            StackTrace = exception?.StackTrace
        };

        WriteLogEntry(logEntry);
    }

    /// <summary>
    /// Registra cambios en la configuración de detección
    /// </summary>
    public void LogConfigurationChange(string regionName, Point oldCoordinates, Point newCoordinates, string? oldColor = null, string? newColor = null)
    {
        var logEntry = new
        {
            Timestamp = DateTime.Now,
            Type = "ConfigurationChange",
            Region = regionName,
            OldCoordinates = new { X = oldCoordinates.X, Y = oldCoordinates.Y },
            NewCoordinates = new { X = newCoordinates.X, Y = newCoordinates.Y },
            OldColor = oldColor,
            NewColor = newColor,
            Message = "Detection configuration updated"
        };

        WriteLogEntry(logEntry);
    }

    /// <summary>
    /// Registra estadísticas de detección
    /// </summary>
    public void LogDetectionStatistics(int totalDetections, int successfulDetections, int falsePositives, TimeSpan sessionDuration)
    {
        var successRate = totalDetections > 0 ? (double)successfulDetections / totalDetections * 100 : 0;

        var logEntry = new
        {
            Timestamp = DateTime.Now,
            Type = "DetectionStatistics",
            TotalDetections = totalDetections,
            SuccessfulDetections = successfulDetections,
            FalsePositives = falsePositives,
            SuccessRate = Math.Round(successRate, 2),
            SessionDuration = sessionDuration.ToString(@"hh\:mm\:ss"),
            Message = "Detection session statistics"
        };

        WriteLogEntry(logEntry);
    }

    /// <summary>
    /// Guarda una captura de pantalla con información de debug
    /// </summary>
    public string SaveDebugScreenshot(Image screenshot, Point detectionPoint, Color detectedColor, string context = "debug")
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var filename = $"{context}_{timestamp}.png";
            var filepath = Path.Combine(_screenshotDirectory, filename);

            // Crear una copia de la imagen con marcadores de debug
            using var debugImage = new Bitmap(screenshot);
            using var graphics = Graphics.FromImage(debugImage);

            // Dibujar cruz roja en el punto de detección
            using var redPen = new Pen(Color.Red, 3);
            var crossSize = 10;
            graphics.DrawLine(redPen,
                detectionPoint.X - crossSize, detectionPoint.Y,
                detectionPoint.X + crossSize, detectionPoint.Y);
            graphics.DrawLine(redPen,
                detectionPoint.X, detectionPoint.Y - crossSize,
                detectionPoint.X, detectionPoint.Y + crossSize);

            // Dibujar información de color
            using var brush = new SolidBrush(Color.White);
            using var font = new Font("Arial", 12, FontStyle.Bold);
            var colorInfo = $"RGB({detectedColor.R},{detectedColor.G},{detectedColor.B}) B={detectedColor.B}";
            var textSize = graphics.MeasureString(colorInfo, font);

            // Fondo semi-transparente para el texto
            using var backgroundBrush = new SolidBrush(Color.FromArgb(128, Color.Black));
            graphics.FillRectangle(backgroundBrush, 10, 10, textSize.Width + 10, textSize.Height + 5);
            graphics.DrawString(colorInfo, font, brush, 15, 12);

            // Guardar imagen
            debugImage.Save(filepath, ImageFormat.Png);

            // Log del screenshot guardado
            LogScreenshotSaved(filename, detectionPoint, detectedColor, context);

            return filepath;
        }
        catch (Exception ex)
        {
            LogDetectionError($"Error saving debug screenshot: {ex.Message}", ex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Registra cuando se guarda un screenshot
    /// </summary>
    private void LogScreenshotSaved(string filename, Point detectionPoint, Color detectedColor, string context)
    {
        var logEntry = new
        {
            Timestamp = DateTime.Now,
            Type = "ScreenshotSaved",
            Filename = filename,
            Context = context,
            DetectionPoint = new { X = detectionPoint.X, Y = detectionPoint.Y },
            DetectedColor = new
            {
                R = detectedColor.R,
                G = detectedColor.G,
                B = detectedColor.B,
                Hex = $"#{detectedColor.R:X2}{detectedColor.G:X2}{detectedColor.B:X2}"
            },
            Message = "Debug screenshot saved"
        };

        WriteLogEntry(logEntry);
    }

    /// <summary>
    /// Escribe una entrada de log en formato JSON
    /// </summary>
    private void WriteLogEntry(object logEntry)
    {
        try
        {
            lock (_lockObject)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonString = JsonSerializer.Serialize(logEntry, jsonOptions);
                File.AppendAllText(_logFilePath, jsonString + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            // Fallback logging to prevent infinite loops
            try
            {
                var fallbackLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: Failed to write log entry: {ex.Message}";
                File.AppendAllText(_logFilePath, fallbackLog + Environment.NewLine);
            }
            catch
            {
                // Si incluso el fallback falla, no hacer nada para evitar excepciones en cascada
            }
        }
    }

    /// <summary>
    /// Obtiene las estadísticas de detección del día actual
    /// </summary>
    public DetectionStatistics GetTodayStatistics()
    {
        try
        {
            if (!File.Exists(_logFilePath))
                return new DetectionStatistics();

            var lines = File.ReadAllLines(_logFilePath);
            var today = DateTime.Today;

            var todayEntries = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    try
                    {
                        return JsonSerializer.Deserialize<Dictionary<string, object>>(line);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(entry => entry != null)
                .Where(entry =>
                {
                    if (entry!.TryGetValue("timestamp", out var timestampObj))
                    {
                        if (DateTime.TryParse(timestampObj.ToString(), out var timestamp))
                        {
                            return timestamp.Date == today;
                        }
                    }
                    return false;
                })
                .ToList();

            var colorDetections = todayEntries.Count(e => e!.ContainsKey("type") && e["type"].ToString() == "ColorDetection");
            var turnDetections = todayEntries.Count(e => e!.ContainsKey("type") && e["type"].ToString() == "TurnDetected");
            var errors = todayEntries.Count(e => e!.ContainsKey("type") && e["type"].ToString() == "DetectionError");

            return new DetectionStatistics
            {
                Date = today,
                TotalColorDetections = colorDetections,
                TurnDetections = turnDetections,
                Errors = errors,
                ScreenshotsSaved = Directory.GetFiles(_screenshotDirectory, $"*{today:yyyyMMdd}*.png").Length
            };
        }
        catch (Exception ex)
        {
            LogDetectionError($"Error getting statistics: {ex.Message}", ex);
            return new DetectionStatistics();
        }
    }

    /// <summary>
    /// Limpia logs antiguos (mantiene solo los últimos 30 días)
    /// </summary>
    public void CleanupOldLogs()
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-30);

            // Limpiar archivos de log antiguos
            var logFiles = Directory.GetFiles(_logDirectory, "detection_*.log");
            foreach (var logFile in logFiles)
            {
                var filename = Path.GetFileNameWithoutExtension(logFile);
                if (filename.Length >= 19) // "detection_yyyyMMdd"
                {
                    var dateString = filename.Substring(10, 8); // Extraer yyyyMMdd
                    if (DateTime.TryParseExact(dateString, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var fileDate))
                    {
                        if (fileDate < cutoffDate)
                        {
                            File.Delete(logFile);
                        }
                    }
                }
            }

            // Limpiar screenshots antiguos
            var screenshots = Directory.GetFiles(_screenshotDirectory, "*.png");
            foreach (var screenshot in screenshots)
            {
                var fileInfo = new FileInfo(screenshot);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(screenshot);
                }
            }
        }
        catch (Exception ex)
        {
            LogDetectionError($"Error cleaning up old logs: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Estadísticas de detección para un día específico
/// </summary>
public class DetectionStatistics
{
    public DateTime Date { get; set; } = DateTime.Today;
    public int TotalColorDetections { get; set; }
    public int TurnDetections { get; set; }
    public int Errors { get; set; }
    public int ScreenshotsSaved { get; set; }

    public double SuccessRate => TotalColorDetections > 0 ? (double)TurnDetections / TotalColorDetections * 100 : 0;
}