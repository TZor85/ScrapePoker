namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Rutas de los fixtures de caracterización. La lectura usa la copia del output;
/// la escritura apunta al árbol de fuentes, que es lo que se versiona.
/// </summary>
internal static class FixturePaths
{
    /// <summary>Copia que el csproj deja junto al ensamblado de tests.</summary>
    public static string ReadRoot { get; } = Path.Combine(AppContext.BaseDirectory, "fixtures");

    /// <summary>Raíz versionada: <c>test/fixtures</c> del repositorio.</summary>
    public static string WriteRoot { get; } = LocateSourceRoot();

    public static string Decisions => Path.Combine(ReadRoot, "decisions");

    public static string Postflop => Path.Combine(Decisions, "postflop");

    public static string Preflop => Path.Combine(Decisions, "preflop");

    public static string StrategyProfile => Path.Combine(Decisions, "strategy-profile.json");

    public static string SourcePostflop => Path.Combine(WriteRoot, "decisions", "postflop");

    public static string SourcePreflop => Path.Combine(WriteRoot, "decisions", "preflop");

    public static string SourceStrategyProfile => Path.Combine(WriteRoot, "decisions", "strategy-profile.json");

    private static string LocateSourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OpenScrape.sln")))
                return Path.Combine(directory.FullName, "test", "fixtures");

            directory = directory.Parent;
        }

        // Fuera del repositorio (por ejemplo, si se corre el ensamblado suelto):
        // se escribe junto al output para no fallar.
        return ReadRoot;
    }
}
