using System.Globalization;

namespace OpenScrape.App.Helpers;

/// <summary>
/// Conversión entre el texto numérico que devuelve el OCR y <see cref="decimal"/>.
/// El OCR lee los importes tal como los muestra el cliente de póker: con coma decimal
/// y sin separador de miles. La cultura del sistema no debe intervenir, porque en un
/// Windows en inglés la coma se interpreta como separador de miles y el importe se lee
/// cien veces más grande.
/// </summary>
public static class OcrNumber
{
    /// <summary>Cultura de referencia para leer y escribir importes del OCR.</summary>
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-ES");

    /// <summary>
    /// Interpreta el texto del OCR. Devuelve false si no es un número.
    /// </summary>
    public static bool TryParse(string? text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Any, Culture, out value);

    /// <summary>
    /// Convierte un importe a texto en la misma cultura con la que se parsea, para que
    /// el ida y vuelta entre decimal y texto no dependa de la máquina.
    /// </summary>
    public static string Format(decimal value) => value.ToString(Culture);
}
