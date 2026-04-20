using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.DTOs;

/// <summary>
/// Entrada inmutable del facade de decisión postflop. Contiene todos los
/// datos necesarios para reproducir una decisión (equity + textura + decisión
/// + sizing) sin depender de estado global.
/// </summary>
public sealed record DecisionRequest
{
    // --- Cartas y mesa ---
    public required IReadOnlyList<CardDataOuts> HeroCards { get; init; }
    public required IReadOnlyList<CardDataOuts> CommunityCards { get; init; }
    public required BoardPosition Street { get; init; }
    public required HandSituation Situation { get; init; }

    // --- Stacks / pot ---
    public decimal HeroStack { get; init; }
    public decimal VillainStack { get; init; }
    public decimal PotSize { get; init; }
    public decimal BetToCall { get; init; }

    // --- Posición y oponentes ---
    public bool IsInPosition { get; init; }
    public int NumOpponents { get; init; } = 1;
    public TablePosition HeroPosition { get; init; } = TablePosition.None;
    public TablePosition VillainPosition { get; init; } = TablePosition.None;

    /// <summary>Identificador del villano activo para lookup en IOpponentTracker (p.ej. "Unknown").</summary>
    public string VillainId { get; init; } = "Unknown";

    /// <summary>Tamaño de apuesta del villano en la calle actual.</summary>
    public BetSizeCategory VillainBetSize { get; init; } = BetSizeCategory.NoBet;

    // --- Estado cross-street (proviene de PostflopGameContext) ---
    public bool HeroIsAggressor { get; init; }
    public bool PreviousStreetBet { get; init; }
    public bool VillainShowedAggression { get; init; }
    public bool VillainBarreling { get; init; }
    public bool VillainAggressorCheckedPreviousStreet { get; init; }
    public bool VillainCheckedMiddleStreet { get; init; }
    public BetSizeCategory VillainBetSizeFlop { get; init; } = BetSizeCategory.NoBet;
    public BetSizeCategory VillainBetSizeTurn { get; init; } = BetSizeCategory.NoBet;
    public bool HeroFloatedFlop { get; init; }
    public bool TurnCalledWithFlushDanger { get; init; }
    public bool HeroCheckedAllStreets { get; init; }
    public bool IsAnyoneAllIn { get; init; }
    public bool IsDonkBet { get; init; }

    /// <summary>Tablero de la calle previa. Necesario para AnalyzeBoardChange en turn/river.</summary>
    public IReadOnlyList<CardDataOuts>? PreviousBoard { get; init; }

    // --- Blockers y flags de mano/board que el caller ya conoce ---
    public bool HeroBlocksDangerSuit { get; init; }
    public bool HeroBlocksTopCard { get; init; }
    public bool HeroHasNutBlocker { get; init; }
    public bool IsBroadwayWet { get; init; }

    /// <summary>Perfil explícito del villano (opcional: si null, el facade lo resuelve vía IOpponentTracker).</summary>
    public OpponentProfile? VillainProfile { get; init; }

    /// <summary>Identificador de la situación preflop, pasado a IPokerCalculator.</summary>
    public string? HandSituationTag { get; init; }

    /// <summary>Override opcional del número de iteraciones MC (null → usa default por street).</summary>
    public int? MonteCarloIterations { get; init; }
}
