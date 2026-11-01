using System.Windows.Forms;

using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Pinta las líneas de log en un <see cref="RichTextBox"/>: el prefijo lleva el color
/// del nivel y el mensaje queda en el negro del tema. El resumen de una mano llega como
/// un único evento con saltos de línea; esas continuaciones se sangran para que no se
/// confundan con el log siguiente, y cada cambio de mano abre con un separador.
/// Es una clase con estado: recuerda la última mano pintada para no repetir el separador.
/// </summary>
public sealed class LogRichTextRenderer
{
    private const string Indent = "    ";

    private long? _lastHand;

    /// <summary>Olvida la mano anterior, para que el próximo bloque vuelva a abrir con separador.</summary>
    public void Reset() => _lastHand = null;

    public void Render(RichTextBox box, LogEntry entry, int maxLines = 0)
        => Render(box, entry, exception: null, maxLines);

    public void Render(RichTextBox box, LogEntry entry, Exception? exception, int maxLines = 0)
    {
        ArgumentNullException.ThrowIfNull(box);

        var (prefix, message) = LogLineFormatter.Split(entry, exception);
        Append(box, prefix, message, LogLineFormatter.ParseLevel(entry.Level), entry.HandNumber, maxLines);
    }

    /// <summary>
    /// Agrega una entrada ya formateada. No mueve el caret: el panel decide si sigue el
    /// stream o si respeta la posición de quien está leyendo hacia arriba.
    /// </summary>
    public void Append(RichTextBox box, string prefix, string message, LogLevel level, long? handNumber, int maxLines = 0)
    {
        ArgumentNullException.ThrowIfNull(box);

        TrimTop(box, maxLines);
        AppendHandSeparator(box, handNumber);

        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionColor = LogLineColors.For(level);
        box.AppendText(prefix);

        box.SelectionColor = LogLineColors.Message;

        var lines = message.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        box.AppendText(' ' + lines[0]);

        for (var i = 1; i < lines.Length; i++)
            box.AppendText(Environment.NewLine + Indent + lines[i].TrimStart());

        box.AppendText(Environment.NewLine);
        box.SelectionLength = 0;
    }

    private void AppendHandSeparator(RichTextBox box, long? handNumber)
    {
        if (handNumber is not { } hand || hand == _lastHand)
            return;

        _lastHand = hand;

        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionColor = LogLineColors.Message;
        box.AppendText($"─── Mano {hand} ───{Environment.NewLine}");
    }

    /// <summary>
    /// Recorta por caracteres, no reasignando <see cref="TextBoxBase.Lines"/>: esa
    /// propiedad borra el formato de todo el control y las líneas viejas perderían color.
    /// </summary>
    private static void TrimTop(RichTextBox box, int maxLines)
    {
        if (maxLines <= 0)
            return;

        var lines = box.Lines;
        if (lines.Length < maxLines)
            return;

        var toRemove = lines.Length - (maxLines - 1);
        var characters = 0;

        for (var i = 0; i < toRemove && i < lines.Length; i++)
            characters += lines[i].Length + Environment.NewLine.Length;

        if (characters <= 0)
            return;

        box.SelectionStart = 0;
        box.SelectionLength = Math.Min(characters, box.TextLength);
        box.SelectedText = string.Empty;
    }
}
