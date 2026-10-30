namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Opciones del <see cref="MartenLoggerProvider"/>, bindeadas a la sección
/// <c>Logging:Marten</c> de <c>appsettings.json</c>.
/// </summary>
public sealed class MartenLoggerOptions
{
    /// <summary>Si es <c>false</c>, el proveedor no acepta eventos.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Días de retención; las entradas más antiguas se eliminan.</summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>Cantidad de entradas agrupadas por escritura.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>Espera máxima para agrupar entradas antes de escribir.</summary>
    public int FlushIntervalMs { get; set; } = 1000;

    /// <summary>Capacidad de la cola en memoria; si se llena, se descartan eventos nuevos.</summary>
    public int ChannelCapacity { get; set; } = 10_000;

    /// <summary>Recorte de mensajes largos para no inflar el documento.</summary>
    public int MaxMessageLength { get; set; } = 8_000;
}
