namespace OpenScrape.App.Services;

/// <summary>
/// Limpia el texto que devuelve el OCR de la región del número de mano.
/// La región puede incluir la etiqueta "ID: " que la mesa dibuja antes del número; con
/// el whitelist de dígitos esa etiqueta se transcribe como números ("ID:" → "10", "1D" → "13")
/// y acababa dentro del identificador guardado. Se corta lo anterior a los dos puntos y,
/// si quedaron varios tramos de dígitos, se usa el más largo.
/// </summary>
public static class HandNumberText
{
    public static string Clean(string? ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return string.Empty;

        var text = ocrText;
        var lastColon = text.LastIndexOf(':');
        if (lastColon >= 0)
            text = text[(lastColon + 1)..];

        return LongestDigitRun(text);
    }

    private static string LongestDigitRun(string text)
    {
        var longest = string.Empty;
        var index = 0;

        while (index < text.Length)
        {
            if (!char.IsDigit(text[index]))
            {
                index++;
                continue;
            }

            var start = index;
            while (index < text.Length && char.IsDigit(text[index]))
                index++;

            var run = text[start..index];
            if (run.Length >= longest.Length)
                longest = run;
        }

        return longest;
    }
}
