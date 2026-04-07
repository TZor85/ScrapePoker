# Spec: PostflopDecisionInput

## Requisito

El sistema DEBE reemplazar los 36 parámetros de `PostflopDecisionService.DetermineAction()` por un record inmutable `PostflopDecisionInput` que encapsule todo el contexto necesario para una decisión postflop.

## Conceptos

- **PostflopDecisionInput**: Record inmutable con todos los campos que antes eran parámetros individuales.
- **Overload legacy**: El método con 36 parámetros se mantiene temporalmente, marcado `[Obsolete]`, delegando al nuevo.
- **Migración gradual**: Los 186+ tests existentes usan el overload legacy sin cambios. Nuevos tests usan PostflopDecisionInput.

## Definición

```csharp
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
}
```

## Escenarios

### Escenario 1: Construcción con object initializer

DADO que GameCoordinator necesita invocar una decisión de flop
CUANDO construye PostflopDecisionInput con:
```csharp
var input = new PostflopDecisionInput
{
    Equity = 65.5,
    Street = BoardPosition.Flop,
    Situation = HandSituation.OpenRaise,
    BoardTexture = "Dry",
    IsInPosition = true,
    VillainBetSize = BetSizeCategory.NoBet,
    HeroHandRank = HandRank.OnePair,
    NumOpponents = 2
};
```
ENTONCES todos los campos no especificados tienen sus defaults (false, 0, etc.)
Y el record es inmutable (no se puede modificar después de creación)

### Escenario 2: Overload legacy delega correctamente

DADO una llamada al método legacy con 36 parámetros
CUANDO se ejecuta `DetermineAction(equity, street, situation, ...)`
ENTONCES internamente construye un PostflopDecisionInput
Y lo pasa al nuevo `DetermineAction(PostflopDecisionInput)`
Y el resultado es idéntico al comportamiento anterior

### Escenario 3: 186+ tests existentes no cambian

DADO los tests en PostflopDecisionServiceTests.cs
CUANDO se ejecuta `dotnet test --filter "PostflopDecision"`
ENTONCES todos pasan sin modificación
Y usan el overload legacy que sigue funcional

### Escenario 4: Equality by value

DADO dos PostflopDecisionInput con los mismos valores
CUANDO se comparan con ==
ENTONCES son iguales (record equality)

### Escenario 5: Immutabilidad con `with`

DADO un PostflopDecisionInput base
CUANDO se crea una variante con `input with { Street = BoardPosition.Turn }`
ENTONCES el input original no cambia
Y la variante tiene Street = Turn y todos los demás campos iguales
