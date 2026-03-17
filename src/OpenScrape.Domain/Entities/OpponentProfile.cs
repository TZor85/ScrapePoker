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

    // Estadísticas calculadas
    public double VPIP => HandsPlayed > 0 ? (double)TimesVoluntarilyPutMoneyIn / HandsPlayed * 100 : 50;
    public double PFR => HandsPlayed > 0 ? (double)TimesPreflopRaised / HandsPlayed * 100 : 15;
    public double ThreeBetPct => HandsPlayed > 0 ? (double)TimesThreeBet / HandsPlayed * 100 : 5;

    /// <summary>
    /// Aggression Factor: (bets + raises) / calls. &gt;1 = agresivo, &lt;1 = pasivo.
    /// </summary>
    public double AggressionFactor
    {
        get
        {
            int aggressive = TimesPostflopBet + TimesPostflopRaised;
            int passive = TimesPostflopCalled;
            if (passive == 0) return aggressive > 0 ? 3.0 : 1.0;
            return (double)aggressive / passive;
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
    /// Mínimo de manos necesario para considerar las estadísticas fiables.
    /// </summary>
    public bool IsReliable => HandsPlayed >= 20;
}

public enum OpponentType
{
    Unknown,
    TAG,  // Tight-Aggressive (reg)
    LAG,  // Loose-Aggressive
    TP,   // Tight-Passive (nit)
    LP    // Loose-Passive (fish)
}
