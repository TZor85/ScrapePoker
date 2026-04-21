namespace OpenScrape.Domain.Exceptions;

/// <summary>
/// Lanzada cuando <c>StrategyProfileValidator</c> detecta errores en el perfil
/// cargado desde <c>appsettings.json</c> al arrancar la aplicación. Acumula todos
/// los errores encontrados en un único mensaje multi-línea para fallar rápido con
/// un diagnóstico completo.
/// </summary>
public sealed class StrategyProfileValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public StrategyProfileValidationException(IReadOnlyList<string> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    private static string BuildMessage(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
            return "StrategyProfile inválido.";

        var lines = new List<string>
        {
            $"StrategyProfile inválido ({errors.Count} error{(errors.Count == 1 ? "" : "es")}):"
        };
        lines.AddRange(errors.Select(e => $"  - {e}"));
        return string.Join(Environment.NewLine, lines);
    }
}
