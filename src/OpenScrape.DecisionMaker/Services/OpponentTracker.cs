using System.Collections.Concurrent;

using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

// Alias explícito para resolver ambigüedad con PositionStats si aparece en otro namespace

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Servicio que acumula estadísticas de oponentes durante la sesión.
/// Mantiene un perfil por jugador (identificado por nombre/seat).
/// </summary>
public class OpponentTracker : Interfaces.IOpponentTracker
{
    private readonly ConcurrentDictionary<string, OpponentProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Obtiene o crea el perfil de un oponente.
    /// </summary>
    public OpponentProfile GetProfile(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return new OpponentProfile();

        return _profiles.GetOrAdd(playerId, id => new OpponentProfile { PlayerId = id });
    }

    /// <summary>
    /// Registra que un jugador participó en una mano.
    /// S22.3: Opcionalmente pasa la posición para acumular stats posicionales.
    /// </summary>
    public void RecordHandPlayed(string playerId, TablePosition position = TablePosition.None)
    {
        var profile = GetProfile(playerId);
        profile.HandsPlayed++;

        if (position != TablePosition.None)
        {
            if (!profile.PositionProfiles.ContainsKey(position))
                profile.PositionProfiles[position] = new OpponentPositionProfile();
            profile.PositionProfiles[position].HandsPlayed++;
        }
    }

    /// <summary>
    /// Registra que un jugador puso dinero voluntariamente preflop (limped o raised).
    /// S22.3: Opcionalmente pasa la posición para stats posicionales.
    /// </summary>
    public void RecordVPIP(string playerId, TablePosition position = TablePosition.None)
    {
        var profile = GetProfile(playerId);
        profile.TimesVoluntarilyPutMoneyIn++;

        if (position != TablePosition.None && profile.PositionProfiles.TryGetValue(position, out var posStats))
            posStats.TimesVPIP++;
    }

    /// <summary>
    /// Registra que un jugador hizo raise preflop.
    /// S22.3: Opcionalmente pasa la posición para stats posicionales.
    /// </summary>
    public void RecordPFR(string playerId, TablePosition position = TablePosition.None)
    {
        var profile = GetProfile(playerId);
        profile.TimesPreflopRaised++;

        if (position != TablePosition.None && profile.PositionProfiles.TryGetValue(position, out var posStats))
            posStats.TimesPFR++;
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
    public void RecordPostflopAction(string playerId, PostflopAction action, bool? isVillainInPosition = null)
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

        // Trackear agresión por posición si se conoce
        if (isVillainInPosition.HasValue)
        {
            bool isAggressive = action is PostflopAction.Bet or PostflopAction.Raise;
            bool isPassive = action == PostflopAction.Call;

            if (isVillainInPosition.Value)
            {
                if (isAggressive) profile.TimesAggressiveIP++;
                else if (isPassive) profile.TimesPassiveIP++;
            }
            else
            {
                if (isAggressive) profile.TimesAggressiveOOP++;
                else if (isPassive) profile.TimesPassiveOOP++;
            }
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
    /// Registra resultado de showdown (S18.3).
    /// </summary>
    public void TrackShowdownResult(string playerId, bool wentToSD, bool wonSD)
    {
        var profile = GetProfile(playerId);
        profile.TimesReachedRiver++;
        if (wentToSD)
        {
            profile.TimesWentToShowdown++;
            if (wonSD) profile.TimesWonAtShowdown++;
        }
    }

    /// <summary>
    /// Registra check-raise del oponente (S18.3).
    /// </summary>
    public void TrackCheckRaise(string playerId, bool didCR, bool hadOpportunity)
    {
        var profile = GetProfile(playerId);
        if (hadOpportunity) profile.TimesCheckRaiseOpportunity++;
        if (didCR) profile.TimesCheckRaised++;
    }

    /// <summary>
    /// Registra donk bet del oponente (S18.3).
    /// </summary>
    public void TrackDonkBet(string playerId, bool didDonk, bool hadOpportunity)
    {
        var profile = GetProfile(playerId);
        if (hadOpportunity) profile.TimesDonkBetOpportunity++;
        if (didDonk) profile.TimesDonkBet++;
    }

    /// <summary>
    /// Registra oportunidad de barrel: villain apostó en flop y puede apostar en turn (S18.2).
    /// </summary>
    public void TrackBarrel(string playerId, bool didBarrel)
    {
        var profile = GetProfile(playerId);
        profile.TimesBarrelOpportunity++;
        if (didBarrel) profile.TimesBarreled++;
    }

    /// <summary>
    /// Calcula el fold equity ajustado según el perfil del oponente.
    /// Oponentes loose/pasivos foldean más → mayor fold equity.
    /// Oponentes tight/agresivos foldean menos → menor fold equity.
    /// </summary>
    public double GetAdjustedFoldEquity(string playerId, double baseFoldEquity)
    {
        var profile = GetProfile(playerId);

        // Usar HasReliableAFData (>= 10 acciones) en vez de IsReliable (>= 20 manos)
        // Permite ajustar fold equity antes cuando hay datos de agresión suficientes
        if (!profile.HasReliableAFData)
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
    /// Retorna % de veces que el villano foldeó ante una apuesta/raise postflop.
    /// Retorna -1 si no hay suficientes situaciones (menos de 10 acciones facing bet).
    /// </summary>
    public double GetFoldToBetPct(string playerId)
    {
        var profile = GetProfile(playerId);
        // Usar HasReliableFoldData (>= 8 acciones) en vez de hardcoded 10
        if (!profile.HasReliableFoldData) return -1;
        int totalFacingAction = profile.TimesPostflopFolded + profile.TimesPostflopCalled + profile.TimesPostflopRaised;
        return (double)profile.TimesPostflopFolded / totalFacingAction * 100.0;
    }

    /// <summary>
    /// Obtiene todos los perfiles registrados en la sesión.
    /// </summary>
    public IReadOnlyDictionary<string, OpponentProfile> AllProfiles => _profiles;

    // === Seat-alias cache: mapeo seat ("P3") → alias real ("PlayerA") ===
    private readonly ConcurrentDictionary<string, string> _seatAliasCache = new(StringComparer.OrdinalIgnoreCase);

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
            _profiles.TryAdd(alias, seatProfile))
        {
            seatProfile.PlayerId = alias;
            _profiles.TryRemove(seatName, out _);
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
