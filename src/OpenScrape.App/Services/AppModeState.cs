namespace OpenScrape.App.Services;

/// <summary>
/// Estado de ejecución compartido entre la UI y los servicios de infraestructura.
/// Hoy sólo distingue el modo test: con capturas grabadas los logs no aportan
/// diagnóstico y no deben gastar el límite de la base.
/// </summary>
public sealed class AppModeState
{
    private int _isTestMode;

    /// <summary>True mientras el usuario trabaja con capturas grabadas (check "Test").</summary>
    public bool IsTestMode
    {
        get => Volatile.Read(ref _isTestMode) != 0;
        set => Volatile.Write(ref _isTestMode, value ? 1 : 0);
    }
}
