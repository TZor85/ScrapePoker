using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.DTOs;

/// <summary>
/// Objeto parámetro inmutable que encapsula todo el contexto necesario
/// para una decisión postflop. Reemplaza los 36 parámetros individuales
/// de DetermineAction().
/// </summary>
public record PostflopDecisionInput
{
    // --- Campos obligatorios ---
    public required double Equity { get; init; }
    public required BoardPosition Street { get; init; }
    public required HandSituation Situation { get; init; }
    public required string BoardTexture { get; init; }
    public required bool IsInPosition { get; init; }
    public required BetSizeCategory VillainBetSize { get; init; }

    // --- Campos con defaults ---
    public double PotOdds { get; init; }
    public int TotalOuts { get; init; }
    public bool PreviousStreetBet { get; init; }
    public bool VillainShowedAggression { get; init; }
    public BoardChangeResult? BoardChange { get; init; }
    public bool HeroBlocksDangerSuit { get; init; }
    public decimal HeroStack { get; init; }
    public decimal PotSize { get; init; }
    public bool HasFlushDraw { get; init; }
    public int NumOpponents { get; init; } = 1;
    public bool HeroIsAggressor { get; init; }
    public HandRank HeroHandRank { get; init; }
    public bool HasComboDraw { get; init; }
    public bool VillainAggressorCheckedPreviousStreet { get; init; }
    public bool VillainBarreling { get; init; }
    public OpponentType VillainType { get; init; }
    public PairClassification PairClassification { get; init; }
    public double FoldEquity { get; init; }
    public BetSizeCategory VillainBetSizeFlop { get; init; }
    public BetSizeCategory VillainBetSizeTurn { get; init; }
    public bool VillainCheckedMiddleStreet { get; init; }
    public bool HeroHasNutBlocker { get; init; }
    public bool HeroFloatedFlop { get; init; }
    public double VillainFoldToBetPct { get; init; } = -1;
    public KickerStrength HeroKickerStrength { get; init; }
    public bool TurnCalledWithFlushDanger { get; init; }
    public bool HeroBlocksTopCard { get; init; }
    public bool HeroCheckedAllStreets { get; init; }
    public bool IsAnyoneAllIn { get; init; }
    public bool IsDonkBet { get; init; }
    public OpponentProfile? VillainProfile { get; init; }
    public TablePosition HeroPosition { get; init; }
    public TablePosition VillainPosition { get; init; }
    public bool IsBroadwayWet { get; init; }

    // S22.1: EffectiveOuts incluye descuento por tainted outs (vs TotalOuts sin descuento)
    // Usado en semi-bluff EV y draw calling; TotalOuts se mantiene para clasificación de draws
    public double EffectiveOuts { get; init; }

    // S22.2: Tipo de carta river (Blank/Neutral/Scare) para ajustar bet sizing y bluff catch
    public RiverCardType RiverCardType { get; init; } = RiverCardType.Neutral;
}
