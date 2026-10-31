namespace OpenScrape.Domain.Entities;

/// <summary>
/// Registro diario de logs: todos los eventos de un día local en un único documento.
/// El <see cref="Id"/> es la fecha local en formato <c>yyyy-MM-dd</c>, para que el
/// día que ve el usuario sea el mismo día que se guarda.
/// </summary>
public class LogDay
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Medianoche local del día que representa el registro.</summary>
    public DateTime Date { get; set; }

    public List<LogEntry> Entries { get; set; } = new();

    /// <summary>
    /// Tamaño estimado del documento en bytes. Alimenta el presupuesto global: no se
    /// puede preguntar a Postgres cuánto ocupa sin una consulta extra por escritura.
    /// </summary>
    public long SizeBytes { get; set; }
}
