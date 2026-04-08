# Spec L5: Combo Draw Bonus Ajustado por Board Texture

## Descripcion

El bonus de equity por combo draw (+6.0 fijo) se aplica uniformemente sin considerar la textura del board. En boards Wet/Monotone, el combo draw compite con draws de otros jugadores y tiene reverse implied odds, por lo que el bonus deberia ser menor. En boards Dry, el combo draw es mas valioso.

## Ubicacion

`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`, lineas 162-165

## Codigo Actual

```csharp
// Lineas 162-165
// Combo draw bonus: flush + straight draw = semi-bluff premium.
// No aplicar si hero ya completo el draw (bonus es para draws pendientes).
if (hasComboDraw && street != BoardPosition.River && heroHandRank < HandRank.Straight)
    effectiveEquity += _profile.ComboDrawEquityBonus; // +6.0 fijo
```

## Parametro Actual

```csharp
// StrategyProfile.cs linea 150
public double ComboDrawEquityBonus { get; set; } = 6.0;
```

## Analisis por Textura

| Textura | Razon | Multiplicador |
|---------|-------|---------------|
| **Dry** (< 15) | Draw raro, hero tiene ventaja de outs exclusivos. Villain no puede tener draw competidor | ×1.2 |
| **SemiDry** (15-35) | Draw poco probable en villain, bonus estandar | ×1.0 |
| **SemiWet** (35-60) | Villain puede tener draws similares, reverse implied odds moderadas | ×0.8 |
| **Wet** (60+) | Draws abundantes, alto riesgo de draw dominado | ×0.6 |
| **Monotone** (3 suited) | Flush draw del hero puede estar dominado por flush mas alto | ×0.5 |

**Ejemplo numerico:**
- Board Dry (Ks-7h-2d): combo draw bonus = 6.0 × 1.2 = 7.2 → hero agresivo
- Board Wet (8s-9s-Th): combo draw bonus = 6.0 × 0.6 = 3.6 → hero conservador
- Board Monotone (3s-7s-Js): combo draw bonus = 6.0 × 0.5 = 3.0 → hero muy conservador

## Fix Propuesto

```csharp
if (hasComboDraw && street != BoardPosition.River && heroHandRank < HandRank.Straight)
{
    double textureMultiplier = simplifiedTexture switch
    {
        "Dry" => _profile.ComboDrawTextureDry,
        "SemiDry" => _profile.ComboDrawTextureSemiDry,
        "SemiWet" => _profile.ComboDrawTextureSemiWet,
        "Wet" => _profile.ComboDrawTextureWet,
        "Monotone" => _profile.ComboDrawTextureMonotone,
        _ => 1.0
    };
    effectiveEquity += _profile.ComboDrawEquityBonus * textureMultiplier;
}
```

**Nota:** `simplifiedTexture` ya esta disponible en el scope de `DetermineAction` (se calcula en linea ~194 con `GetSimplifiedTexture()`). Solo hay que mover la lectura del combo draw bonus DESPUES de calcular la textura, o pasar la textura al bloque.

## Nuevos Parametros en StrategyProfile

```csharp
// Combo draw texture multipliers
public double ComboDrawTextureDry { get; set; } = 1.2;
public double ComboDrawTextureSemiDry { get; set; } = 1.0;
public double ComboDrawTextureSemiWet { get; set; } = 0.8;
public double ComboDrawTextureWet { get; set; } = 0.6;
public double ComboDrawTextureMonotone { get; set; } = 0.5;
```

## Dependencia de Orden

Actualmente el combo draw bonus (linea 162) se aplica **antes** de calcular `simplifiedTexture` (linea ~194). El fix requiere:
1. Mover el calculo de `simplifiedTexture` antes del combo draw bonus, O
2. Calcular la textura en un paso previo y reutilizar

La opcion 1 es preferible: reordenar el bloque de combo draw despues de obtener `simplifiedTexture`.

## Escenarios BDD

### Escenario 1: Combo draw en board Dry — bonus aumentado
```
Dado hero con combo draw (flush + straight), street Flop, heroHandRank OnePair
Y board texture Dry (wetness < 15)
Cuando se calcula effectiveEquity
Entonces bonus = 6.0 × 1.2 = 7.2 añadido a effectiveEquity
```

### Escenario 2: Combo draw en board SemiDry — bonus estandar
```
Dado hero con combo draw, street Turn, heroHandRank HighCard
Y board texture SemiDry
Cuando se calcula effectiveEquity
Entonces bonus = 6.0 × 1.0 = 6.0 añadido a effectiveEquity
```

### Escenario 3: Combo draw en board Wet — bonus reducido
```
Dado hero con combo draw, street Flop, heroHandRank HighCard
Y board texture Wet (wetness >= 60)
Cuando se calcula effectiveEquity
Entonces bonus = 6.0 × 0.6 = 3.6 añadido a effectiveEquity
```

### Escenario 4: Combo draw en board Monotone — bonus minimo
```
Dado hero con combo draw, street Flop, heroHandRank HighCard
Y board texture Monotone
Cuando se calcula effectiveEquity
Entonces bonus = 6.0 × 0.5 = 3.0 añadido a effectiveEquity
```

### Escenario 5: Sin combo draw — sin bonus (regresion)
```
Dado hero SIN combo draw
Cuando se calcula effectiveEquity
Entonces no se añade ningun bonus de combo draw
```

### Escenario 6: Combo draw en river — sin bonus (regresion)
```
Dado hero con combo draw, street River
Cuando se calcula effectiveEquity
Entonces no se añade bonus (river = no draws pendientes)
```

### Escenario 7: Hero ya completo draw — sin bonus (regresion)
```
Dado hero con heroHandRank >= Straight
Y hasComboDraw = true (flag residual)
Cuando se calcula effectiveEquity
Entonces no se añade bonus (hero ya tiene mano hecha)
```

## Tests Requeridos

1. **Test_ComboDraw_DryBoard_IncreasedBonus** — Dry → ×1.2
2. **Test_ComboDraw_SemiDryBoard_StandardBonus** — SemiDry → ×1.0
3. **Test_ComboDraw_SemiWetBoard_ReducedBonus** — SemiWet → ×0.8
4. **Test_ComboDraw_WetBoard_ReducedBonus** — Wet → ×0.6
5. **Test_ComboDraw_MonotoneBoard_MinimumBonus** — Monotone → ×0.5
6. **Test_NoComboDraw_NoBonus** — regresion
7. **Test_ComboDraw_River_NoBonus** — regresion
8. **Test_ComboDraw_MadeHand_NoBonus** — regresion: HandRank >= Straight
