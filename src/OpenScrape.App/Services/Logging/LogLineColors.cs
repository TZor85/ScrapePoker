using System.Drawing;

using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Paleta del panel de logs. Los colores están pensados para el fondo claro del
/// panel: el objetivo es ubicar el nivel de un vistazo sin que el texto pierda
/// contraste (sobre blanco, los tonos pastel quedaban lavados).
/// </summary>
public static class LogLineColors
{
    /// <summary>Negro suave para el cuerpo del mensaje sobre el fondo claro.</summary>
    public static Color Message { get; } = Color.FromArgb(26, 26, 26);

    /// <summary>Color del prefijo de un evento según su nivel.</summary>
    public static Color For(LogLevel level) => level switch
    {
        LogLevel.Critical or LogLevel.Error => Color.FromArgb(178, 58, 58),
        LogLevel.Warning => Color.FromArgb(138, 97, 0),
        LogLevel.Debug => Color.FromArgb(31, 95, 168),
        LogLevel.Trace => Color.FromArgb(47, 107, 44),
        _ => Message
    };
}
