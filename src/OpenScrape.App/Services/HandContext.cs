namespace OpenScrape.App.Services;

/// <summary>
/// Mano en curso, compartida entre el <see cref="GameLoggerService"/> que la abre y
/// los loggers que la estampan. El <c>BeginScope</c> no sirve para esto: vive en el
/// contexto de ejecución de <c>StartNewHandAsync</c> y no alcanza a quien loguea
/// después, así que los eventos de HandProcessor, TableLayoutService y FrmMain
/// quedaban sin mano.
/// </summary>
public sealed class HandContext
{
    // Los números de mano son siempre positivos; long.MinValue sirve de "sin mano".
    private const long NoHand = long.MinValue;

    private long _current = NoHand;

    /// <summary>Número de mano en curso, o <c>null</c> si no hay ninguna abierta.</summary>
    public long? Current
    {
        get
        {
            var value = Interlocked.Read(ref _current);
            return value == NoHand ? null : value;
        }

        set => Interlocked.Exchange(ref _current, value ?? NoHand);
    }
}
