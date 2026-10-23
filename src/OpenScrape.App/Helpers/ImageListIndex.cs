namespace OpenScrape.App.Helpers;

/// <summary>
/// Resuelve qué posición ocupa el archivo elegido dentro de la lista de imágenes
/// de la carpeta. <see cref="Forms.FormImage"/> la usa al cargar para que la
/// imagen mostrada sea la del diálogo y no la que dejó el índice anterior.
/// </summary>
public static class ImageListIndex
{
    /// <summary>
    /// Devuelve el índice de <paramref name="selectedPath"/> dentro de
    /// <paramref name="files"/>. Sin coincidencia, sin selección o con lista
    /// vacía devuelve 0, que es el primer elemento disponible.
    /// </summary>
    public static int FindIndex(IReadOnlyList<string> files, string? selectedPath)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (files.Count == 0 || string.IsNullOrWhiteSpace(selectedPath))
            return 0;

        for (var i = 0; i < files.Count; i++)
        {
            if (string.Equals(files[i], selectedPath, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return 0;
    }
}
