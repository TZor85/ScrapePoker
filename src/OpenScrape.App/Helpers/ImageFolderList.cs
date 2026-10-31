namespace OpenScrape.App.Helpers;

/// <summary>
/// Lista las imágenes de la carpeta que contiene un archivo elegido.
/// Reemplaza el corte por <c>"game"</c> sobre el path, que matcheaba el segmento
/// <c>games</c> de la ruta y devolvía los PNG del nivel superior.
/// </summary>
public static class ImageFolderList
{
    /// <summary>
    /// Devuelve los <c>*.png</c> de la carpeta de <paramref name="selectedPath"/>,
    /// ordenados por nombre. Devuelve vacío si la ruta no tiene carpeta o no existe.
    /// </summary>
    public static IReadOnlyList<string> Resolve(string? selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath))
            return Array.Empty<string>();

        var directory = Path.GetDirectoryName(selectedPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return Array.Empty<string>();

        return Directory.GetFiles(directory, "*.png")
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
