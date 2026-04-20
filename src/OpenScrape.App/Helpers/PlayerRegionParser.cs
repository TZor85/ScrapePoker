using System.Text.RegularExpressions;

namespace OpenScrape.App.Helpers;

/// <summary>
/// Parser de nombres de región del tablemap (p.ej. "p3bet", "p11stack") para
/// extraer el número de jugador. Extraído de <c>FrmMain.GetPlayerNumber</c>.
/// </summary>
public static class PlayerRegionParser
{
    /// <summary>
    /// Extrae el número de jugador del nombre de una región.
    /// </summary>
    /// <param name="regionName">Nombre de la región (p.ej. "p3bet", "p11stack"). Puede ser null o vacío.</param>
    /// <param name="extraText">Sufijo esperado tras el número (p.ej. "bet", "stack"). Vacío para aceptar sólo el número.</param>
    /// <returns>El número de jugador si el nombre matchea el patrón; <c>null</c> en caso contrario.</returns>
    public static int? GetPlayerNumber(string? regionName, string extraText = "")
    {
        if (string.IsNullOrEmpty(regionName))
            return null;

        var match = Regex.Match(regionName, @$"p(\d+){extraText}");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }
}
