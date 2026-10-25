namespace OpenScrape.App.Services.State;

/// <summary>
/// Contexto de estado cross-street para decisiones postflop. Record inmutable:
/// cada transición produce una instancia nueva vía <c>with</c> o helpers
/// <c>WithFlopState</c>/<c>WithTurnState</c>/<c>TrackHeroStack</c>. El contexto
/// vive en <see cref="OpenScrape.App.Services.IPostflopContextHolder"/> como
/// único poseedor durante la mano.
/// </summary>
public sealed record PostflopGameContext
{
    // === Estado cross-street: quién apostó en qué calle ===
    public bool VillainBetFlop { get; init; }
    public bool VillainBetTurn { get; init; }
    public bool HeroBetFlop { get; init; }
    public bool HeroBetTurn { get; init; }
    public bool PreviousStreetWasBet { get; init; }

    // === Tamaño de apuesta del villano por street (sizing tells) ===
    public BetSizeCategory VillainBetSizeFlop { get; init; } = BetSizeCategory.NoBet;
    public BetSizeCategory VillainBetSizeTurn { get; init; } = BetSizeCategory.NoBet;

    /// <summary>
    /// El agresor preflop checkeó en el flop → señal de debilidad para probe bet.
    /// </summary>
    public bool VillainAggressorCheckedFlop { get; init; }

    /// <summary>
    /// El villano apostó en flop pero checkeó en turn → patrón bet-check-bet si apuesta en river.
    /// Indica debilidad (draw fallido que reintenta) vs barrel real (rango fuerte).
    /// </summary>
    public bool VillainCheckedMiddleStreet { get; init; }

    /// <summary>
    /// Hero floateó en flop (call con aire + posición) → en turn debe apostar si villano chequea.
    /// </summary>
    public bool HeroFloatedFlop { get; init; }

    /// <summary>
    /// Hero calleó turn con flush draw peligroso en board (3+ same suit).
    /// Si river completa el flush → check automático.
    /// </summary>
    public bool TurnCalledWithFlushDanger { get; init; }

    /// <summary>
    /// Hero no apostó en ninguna calle previa (flop check, turn check).
    /// En river, puede apostar delayed value con mano decente.
    /// </summary>
    public bool HeroCheckedAllStreets => !HeroBetFlop && !HeroBetTurn;

    /// <summary>
    /// Algún oponente ya comprometió todo su stack (all-in).
    /// Desactiva fold equity y reverse implied odds para ese jugador.
    /// </summary>
    public bool IsAnyoneAllIn { get; init; }

    /// <summary>
    /// S22.4: Turn bet comprometería el river (projected SPR < StackoffProjectedSPRThreshold).
    /// Indica que hero debe ir all-in ahora (o check para control) en vez de tamaño intermedio.
    /// </summary>
    public bool TurnBetCommitsToRiver { get; init; }

    /// <summary>
    /// Detecta si el villano está barreling (apostó en 2+ calles consecutivas).
    /// </summary>
    public bool IsVillainBarreling => VillainBetFlop && VillainBetTurn;

    /// <summary>
    /// Stack del hero al inicio de la mano, antes de cualquier auto-rebuy.
    /// Un subsiguiente aumento brusco (≥50 BB) se interpreta como rebuy
    /// automático de la sala a 100BB y se ignora para no contaminar el
    /// cálculo de profit por mano.
    /// 0 indica que aún no se ha registrado el stack inicial de la mano.
    /// </summary>
    public decimal HeroStackPreRebuy { get; init; }

    /// <summary>
    /// Umbral a partir del cual un aumento de stack se considera auto-rebuy.
    /// </summary>
    private const decimal AutoRebuyThreshold = 50m;

    /// <summary>
    /// Factory que devuelve un contexto en estado inicial para una nueva mano.
    /// Sustituye al antiguo <c>Reset()</c>.
    /// </summary>
    public static PostflopGameContext NewHand() => new();

    /// <summary>
    /// Devuelve un contexto nuevo con el estado actualizado al finalizar el flop.
    /// Centraliza las derivaciones para mantener invariantes coherentes.
    /// </summary>
    public PostflopGameContext WithFlopState(bool heroBet, bool villainBet, bool isPreflopAggressor) => this with
    {
        HeroBetFlop = heroBet,
        VillainBetFlop = villainBet,
        PreviousStreetWasBet = heroBet,
        VillainAggressorCheckedFlop = !isPreflopAggressor && !villainBet
    };

    /// <summary>
    /// Devuelve un contexto nuevo con el estado actualizado al finalizar el turn.
    /// Deriva <c>VillainCheckedMiddleStreet</c> a partir del estado de flop.
    /// </summary>
    public PostflopGameContext WithTurnState(bool heroBet, bool villainBet) => this with
    {
        HeroBetTurn = heroBet,
        VillainBetTurn = villainBet,
        PreviousStreetWasBet = heroBet,
        VillainCheckedMiddleStreet = VillainBetFlop && !villainBet
    };

    /// <summary>
    /// Detecta auto-rebuy sin mutar el receptor. Devuelve:
    /// <list type="bullet">
    /// <item><c>NewContext</c> con <c>HeroStackPreRebuy</c> actualizado según reglas.</item>
    /// <item><c>EffectiveStack</c>: el stack a usar para profit tracking (pre-rebuy si hubo rebuy, actual si no).</item>
    /// </list>
    /// </summary>
    public (PostflopGameContext NewContext, decimal EffectiveStack) TrackHeroStack(decimal currentStack)
    {
        if (HeroStackPreRebuy <= 0)
            return (this with { HeroStackPreRebuy = currentStack }, currentStack);

        if (currentStack > HeroStackPreRebuy &&
            currentStack - HeroStackPreRebuy >= AutoRebuyThreshold)
        {
            // Rebuy detectado: preservar pre-rebuy, devolver su valor como efectivo.
            return (this, HeroStackPreRebuy);
        }

        // Actualización normal (descenso o incremento pequeño).
        return (this with { HeroStackPreRebuy = currentStack }, currentStack);
    }
}
