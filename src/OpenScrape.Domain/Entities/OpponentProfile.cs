namespace OpenScrape.Domain.Entities;

/// <summary>
/// Perfil estadístico de un oponente acumulado durante la sesión.
/// VPIP = Voluntarily Put $ In Pot, PFR = Pre-Flop Raise, AF = Aggression Factor.
/// </summary>
public class OpponentProfile
{
    public string PlayerId { get; set; } = string.Empty;
    public int HandsPlayed { get; set; }

    // Contadores preflop
    public int TimesVoluntarilyPutMoneyIn { get; set; }
    public int TimesPreflopRaised { get; set; }
    public int TimesThreeBet { get; set; }

    // Contadores postflop
    public int TimesPostflopBet { get; set; }
    public int TimesPostflopRaised { get; set; }
    public int TimesPostflopCalled { get; set; }
    public int TimesPostflopFolded { get; set; }

    // Contadores especiales
    public int TimesCBet { get; set; }
    public int TimesCBetOpportunity { get; set; }
    public int TimesFoldedToCBet { get; set; }
    public int TimesFacedCBet { get; set; }

    // Contadores postflop por posición (IP vs OOP)
    public int TimesAggressiveIP { get; set; }   // bet + raise estando IP
    public int TimesPassiveIP { get; set; }       // call estando IP
    public int TimesAggressiveOOP { get; set; }   // bet + raise estando OOP
    public int TimesPassiveOOP { get; set; }      // call estando OOP

    // Estadísticas calculadas
    public double VPIP => HandsPlayed > 0 ? (double)TimesVoluntarilyPutMoneyIn / HandsPlayed * 100 : 50;
    public double PFR => HandsPlayed > 0 ? (double)TimesPreflopRaised / HandsPlayed * 100 : 15;
    public double ThreeBetPct => HandsPlayed > 0 ? (double)TimesThreeBet / HandsPlayed * 100 : 5;

    /// <summary>
    /// Aggression Factor con Laplace smoothing: (aggressive+1)/(passive+1).
    /// Evita cliff cuando passive=0 y regresa a AF=1.0 con pocas muestras.
    /// </summary>
    public double AggressionFactor
    {
        get
        {
            int aggressive = TimesPostflopBet + TimesPostflopRaised;
            int passive = TimesPostflopCalled;
            return (double)(aggressive + 1) / (passive + 1);
        }
    }

    /// <summary>
    /// Aggression Factor estando In Position. -1 si datos insuficientes.
    /// </summary>
    public double AggressionFactorIP
    {
        get
        {
            if (TimesAggressiveIP + TimesPassiveIP < 5) return -1;
            return (double)(TimesAggressiveIP + 1) / (TimesPassiveIP + 1);
        }
    }

    /// <summary>
    /// Aggression Factor estando Out of Position. -1 si datos insuficientes.
    /// </summary>
    public double AggressionFactorOOP
    {
        get
        {
            if (TimesAggressiveOOP + TimesPassiveOOP < 5) return -1;
            return (double)(TimesAggressiveOOP + 1) / (TimesPassiveOOP + 1);
        }
    }

    /// <summary>
    /// Fold to C-Bet %: qué tan frecuente foldea ante continuation bet.
    /// </summary>
    public double FoldToCBetPct => TimesFacedCBet > 0
        ? (double)TimesFoldedToCBet / TimesFacedCBet * 100
        : 50;

    /// <summary>
    /// C-Bet %: qué tan frecuente hace continuation bet cuando tiene oportunidad.
    /// </summary>
    public double CBetPct => TimesCBetOpportunity > 0
        ? (double)TimesCBet / TimesCBetOpportunity * 100
        : 50;

    /// <summary>
    /// Clasificación simple del oponente basada en VPIP y PFR.
    /// </summary>
    public OpponentType Type
    {
        get
        {
            if (HandsPlayed < 10) return OpponentType.Unknown;

            bool isLoose = VPIP > 30;
            bool isAggressive = AggressionFactor > 1.5;

            return (isLoose, isAggressive) switch
            {
                (true, true) => OpponentType.LAG,    // Loose-Aggressive
                (true, false) => OpponentType.LP,     // Loose-Passive (fish)
                (false, true) => OpponentType.TAG,    // Tight-Aggressive (reg)
                (false, false) => OpponentType.TP,    // Tight-Passive (nit)
            };
        }
    }

    /// <summary>
    /// Tipo del oponente considerando si está IP o OOP.
    /// Villano puede ser LAG IP pero TAG OOP. Usa AF posicional si hay datos, sino global.
    /// </summary>
    public OpponentType GetTypeForPosition(bool villainIsInPosition)
    {
        if (HandsPlayed < 10) return OpponentType.Unknown;

        double af = villainIsInPosition ? AggressionFactorIP : AggressionFactorOOP;
        // Fallback a AF global si no hay datos posicionales suficientes
        if (af < 0) af = AggressionFactor;

        bool isLoose = VPIP > 30;
        bool isAggressive = af > 1.5;

        return (isLoose, isAggressive) switch
        {
            (true, true) => OpponentType.LAG,
            (true, false) => OpponentType.LP,
            (false, true) => OpponentType.TAG,
            (false, false) => OpponentType.TP,
        };
    }

    /// <summary>
    /// Mínimo de manos necesario para considerar el perfil global fiable.
    /// </summary>
    public bool IsReliable => HandsPlayed >= 20;

    /// <summary>
    /// Datos de C-bet fiables: al menos 5 oportunidades de c-bet vistas.
    /// Permite usar CBetPct y FoldToCBetPct antes de 20 manos.
    /// </summary>
    public bool HasReliableCBetData => TimesCBetOpportunity >= 5 && TimesFacedCBet >= 5;

    /// <summary>
    /// Datos de agresión fiables: al menos 10 acciones postflop (bet/raise/call).
    /// Permite usar AggressionFactor antes de 20 manos.
    /// </summary>
    public bool HasReliableAFData =>
        (TimesPostflopBet + TimesPostflopRaised + TimesPostflopCalled) >= 10;

    /// <summary>
    /// Datos de fold to bet fiables: al menos 8 situaciones facing action.
    /// </summary>
    public bool HasReliableFoldData =>
        (TimesPostflopFolded + TimesPostflopCalled + TimesPostflopRaised) >= 8;

    /// <summary>
    /// Datos de VPIP/PFR fiables: al menos 10 manos (más rápido que IsReliable).
    /// </summary>
    public bool HasReliablePreflopData => HandsPlayed >= 10;
}

public enum OpponentType
{
    Unknown,
    TAG,  // Tight-Aggressive (reg)
    LAG,  // Loose-Aggressive
    TP,   // Tight-Passive (nit)
    LP    // Loose-Passive (fish)
}
