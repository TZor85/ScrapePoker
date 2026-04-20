using OpenScrape.App.Entities;

namespace OpenScrape.App.Services;

/// <summary>
/// Formatter de acciones preflop que convierte strings con multiplicador
/// ("3Bet x6") en strings enriquecidos con la cantidad total en big blinds
/// ("3Bet x6 (30BB)"). Extraído de <c>FrmMain.EnrichActionWithBBAmount</c>.
/// </summary>
public interface IActionFormatter
{
    /// <summary>
    /// Enriquece una acción preflop anexando el monto total en big blinds.
    /// La base del cálculo es la mayor apuesta entre <paramref name="villains"/>;
    /// si ningún villano ha apostado, se usa <paramref name="bigBlind"/> como base.
    /// </summary>
    /// <param name="action">Acción a enriquecer (p.ej. "3Bet x6", "Open Raise x2.4").</param>
    /// <param name="villains">Snapshot de villanos (P0 NO debe estar incluido).</param>
    /// <param name="bigBlind">Big blind actual. Si es ≤ 0 se usa fallback de 0.5.</param>
    /// <returns>Acción original si no contiene multiplicador o es inválida; acción + "(NBB)" en otro caso.</returns>
    string EnrichActionWithBBAmount(string action, IEnumerable<Player> villains, decimal bigBlind);
}
