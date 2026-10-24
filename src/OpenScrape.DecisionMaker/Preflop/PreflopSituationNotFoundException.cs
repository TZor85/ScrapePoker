namespace OpenScrape.DecisionMaker.Preflop;

/// <summary>
/// Se lanza cuando la tabla de rangos no tiene un escenario para la situación pedida.
/// La API la traduce a un error de contrato (422) en lugar de decidir con datos faltantes.
/// </summary>
public sealed class PreflopSituationNotFoundException(string situation)
    : Exception($"No hay escenario preflop para la situación '{situation}'.")
{
    public string Situation { get; } = situation;
}
