using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Opciones de configuración del <see cref="TextBoxLoggerProvider"/>.
/// Se bindea a la sección <c>Logging:TextBoxSink</c> de <c>appsettings.json</c>.
/// </summary>
public sealed class TextBoxLoggerOptions
{
    /// <summary>Nivel mínimo que el sink acepta. Eventos por debajo se descartan sin renderizar.</summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>Máximo de líneas visibles en el <c>TextBox</c>; al superarlo se retiran las primeras.</summary>
    public int MaxLines { get; set; } = 5000;

    /// <summary>Si <c>true</c>, los logs emitidos antes de registrar el <c>TextBox</c> destino se guardan en un buffer circular y se vuelcan al registrar.</summary>
    public bool BufferUntilTargetReady { get; set; }

    /// <summary>Capacidad del buffer circular usado cuando <see cref="BufferUntilTargetReady"/> está activo.</summary>
    public int BufferCapacity { get; set; } = 256;
}
