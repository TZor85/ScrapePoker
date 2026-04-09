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

    // Contadores showdown (S18.3)
    public int TimesReachedRiver { get; set; }
    public int TimesWentToShowdown { get; set; }
    public int TimesWonAtShowdown { get; set; }

    // Contadores check-raise (S18.3)
    public int TimesCheckRaised { get; set; }
    public int TimesCheckRaiseOpportunity { get; set; }

    // Contadores donk bet (S18.3)
    public int TimesDonkBet { get; set; }
    public int TimesDonkBetOpportunity { get; set; }

    // Contadores barrel (S18.2)
    public int TimesBarreled { get; set; }
    public int TimesBarrelOpportunity { get; set; }

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

    // Estadísticas ampliadas (S18.3)
    public double WTSDPct => TimesReachedRiver > 0
        ? (double)TimesWentToShowdown / TimesReachedRiver * 100 : 35;

    public double WSDPct => TimesWentToShowdown > 0
        ? (double)TimesWonAtShowdown / TimesWentToShowdown * 100 : 50;

    public double CheckRaisePct => TimesCheckRaiseOpportunity > 0
        ? (double)TimesCheckRaised / TimesCheckRaiseOpportunity * 100 : 8;

    public double DonkBetPct => TimesDonkBetOpportunity > 0
        ? (double)TimesDonkBet / TimesDonkBetOpportunity * 100 : 10;

    // Barrel frequency (S18.2)
    public double BarrelFrequency => TimesBarrelOpportunity > 0
        ? (double)TimesBarreled / TimesBarrelOpportunity * 100 : -1;

    // Reliability checks para stats ampliados
    public bool HasReliableWTSDData => TimesReachedRiver >= 15;
    public bool HasReliableWSDData => TimesWentToShowdown >= 10;
    public bool HasReliableCheckRaiseData => TimesCheckRaiseOpportunity >= 10;
    public bool HasReliableDonkBetData => TimesDonkBetOpportunity >= 8;
    public bool HasReliableBarrelData => TimesBarrelOpportunity >= 8;

    /// <summary>
    /// Frecuencia de barrel esperada por tipo de oponente.
    /// </summary>
    public double ExpectedBarrelFrequency => Type switch
    {
        OpponentType.LAG => 60,
        OpponentType.TAG => 30,
        OpponentType.LP => 20,
        OpponentType.TP => 10,
        _ => 30
    };

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
