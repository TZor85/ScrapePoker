namespace OpenScrape.Domain.Entities;

/// <summary>
/// Evento de log persistido en Marten. Guarda los campos estructurados del
/// evento (categoría, nivel, excepción y scopes) en lugar del texto ya
/// formateado, para poder filtrar y correlacionar sesiones y manos por SQL.
/// </summary>
public class LogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public string Level { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int EventId { get; set; }

    public int ThreadId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// Scopes activos al momento del evento (SessionId, TableName, HandNumber...).
    /// </summary>
    public Dictionary<string, string> Scopes { get; set; } = new();
}
