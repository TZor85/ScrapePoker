using OpenScrape.Domain.Entities;

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Servicio que acumula estadísticas de oponentes durante la sesión.
/// Mantiene un perfil por jugador (identificado por nombre/seat).
/// </summary>
public class OpponentTracker
{
    private readonly Dictionary<string, OpponentProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Obtiene o crea el perfil de un oponente.
    /// </summary>
    public OpponentProfile GetProfile(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return new OpponentProfile();

        if (!_profiles.TryGetValue(playerId, out var profile))
        {
            profile = new OpponentProfile { PlayerId = playerId };
            _profiles[playerId] = profile;
        }

        return profile;
    }

    /// <summary>
    /// Registra que un jugador participó en una mano.
    /// </summary>
    public void RecordHandPlayed(string playerId)
    {
        var profile = GetProfile(playerId);
        profile.HandsPlayed++;
    }

    /// <summary>
    /// Registra que un jugador puso dinero voluntariamente preflop (limped o raised).
    /// </summary>
    public void RecordVPIP(string playerId)
    {
        var profile = GetProfile(playerId);
        profile.TimesVoluntarilyPutMoneyIn++;
    }

    /// <summary>
    /// Registra que un jugador hizo raise preflop.
    /// </summary>
    public void RecordPFR(string playerId)
    {
        var profile = GetProfile(playerId);
        profile.TimesPreflopRaised++;
    }

    /// <summary>
    /// Registra que un jugador hizo 3-bet.
    /// </summary>
    public void RecordThreeBet(string playerId)
    {
        var profile = GetProfile(playerId);
        profile.TimesThreeBet++;
    }

    /// <summary>
    /// Registra una acción postflop del oponente.
    /// </summary>
    public void RecordPostflopAction(string playerId, PostflopAction action)
    {
        var profile = GetProfile(playerId);
        switch (action)
        {
            case PostflopAction.Bet:
                profile.TimesPostflopBet++;
                break;
            case PostflopAction.Raise:
                profile.TimesPostflopRaised++;
                break;
            case PostflopAction.Call:
                profile.TimesPostflopCalled++;
                break;
            case PostflopAction.Fold:
                profile.TimesPostflopFolded++;
                break;
        }
    }

    /// <summary>
    /// Registra una oportunidad de C-Bet (el jugador fue el agresor preflop).
    /// </summary>
    public void RecordCBetOpportunity(string playerId, bool didCBet)
    {
        var profile = GetProfile(playerId);
        profile.TimesCBetOpportunity++;
        if (didCBet) profile.TimesCBet++;
    }

    /// <summary>
    /// Registra que el jugador enfrentó un C-Bet.
    /// </summary>
    public void RecordFacedCBet(string playerId, bool folded)
    {
        var profile = GetProfile(playerId);
        profile.TimesFacedCBet++;
        if (folded) profile.TimesFoldedToCBet++;
    }

    /// <summary>
    /// Calcula el fold equity ajustado según el perfil del oponente.
    /// Oponentes loose/pasivos foldean más → mayor fold equity.
    /// Oponentes tight/agresivos foldean menos → menor fold equity.
    /// </summary>
    public double GetAdjustedFoldEquity(string playerId, double baseFoldEquity)
    {
        var profile = GetProfile(playerId);
        if (!profile.IsReliable)
            return baseFoldEquity;

        return profile.Type switch
        {
            OpponentType.LP => baseFoldEquity * 1.25,  // Fish foldea mucho postflop
            OpponentType.TP => baseFoldEquity * 1.10,  // Nit foldea sin mano fuerte
            OpponentType.TAG => baseFoldEquity * 0.85,  // Reg resiste más
            OpponentType.LAG => baseFoldEquity * 0.70,  // LAG no foldea fácil
            _ => baseFoldEquity,
        };
    }

    /// <summary>
    /// Obtiene todos los perfiles registrados en la sesión.
    /// </summary>
    public IReadOnlyDictionary<string, OpponentProfile> AllProfiles => _profiles;

    // === Seat-alias cache: mapeo seat ("P3") → alias real ("PlayerA") ===
    private readonly Dictionary<string, string> _seatAliasCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registra asociación seat → alias. Si existe perfil por seat name, lo migra al alias.
    /// </summary>
    public void RegisterSeatAlias(string seatName, string alias)
    {
        if (string.IsNullOrWhiteSpace(seatName) || string.IsNullOrWhiteSpace(alias))
            return;

        _seatAliasCache[seatName] = alias;

        // Migrar perfil de seat a alias si existe (solo si no hay perfil con el alias)
        if (_profiles.TryGetValue(seatName, out var seatProfile) &&
            !_profiles.ContainsKey(alias))
        {
            seatProfile.PlayerId = alias;
            _profiles[alias] = seatProfile;
            _profiles.Remove(seatName);
        }
    }

    /// <summary>
    /// Resuelve un seat name a su alias conocido (o null si no hay).
    /// </summary>
    public string? ResolveName(string seatName)
    {
        return _seatAliasCache.TryGetValue(seatName, out var alias) ? alias : null;
    }

    /// <summary>
    /// Limpia todos los perfiles y cache (inicio de nueva sesión).
    /// </summary>
    public void Reset()
    {
        _profiles.Clear();
        _seatAliasCache.Clear();
    }
}

public enum PostflopAction
{
    Bet,
    Raise,
    Call,
    Fold
}
