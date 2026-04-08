# Proposal: Refinamiento del Motor de Decisiones (L1-L6)

## Contexto

Tras completar los bugfixes críticos (BF1-BF3), mejoras de alta prioridad (H1-H5), y estabilidad del game loop (M2, M5), quedan **6 issues de baja prioridad** identificados durante el análisis exhaustivo del motor de decisiones (2026-04-07). Cada uno afecta la precisión en spots específicos pero no compromete la funcionalidad general.

Estos refinamientos representan la última capa de calibración del motor antes de pasar a las fases de automatización (Fase 3) y análisis (Fase 4).

---

## L1: Backdoor Outs Hardcoded a 1

**Archivo:** `OutsCalculator.cs:207-249`
**Severidad:** Baja
**Impacto:** Subestima equity en flop con backdoor draws por ~1-2%

**El problema:**
```csharp
// Línea 207
backdoorOuts += 1; // ~1.5 outs implícitos, redondeado a 1

// Línea 249
backdoorOuts += 1; // ~1 out implícito
```

Ambos backdoor draws (flush y straight) están hardcodeados a exactamente 1 out cada uno:
- **Backdoor flush draw** (3 suited): Probabilidad real runner-runner = ~4.2%, equivalente a **~1.5 outs** implícitos
- **Backdoor straight draw** (3 en ventana de 5): Probabilidad real varía de **~1.0 a ~1.5 outs** según gaps

**Consecuencia:** En flop con ambos backdoor draws, hero tiene ~2 outs reales en vez de los 2.5-3 que debería. Esto infravalora spots de semi-bluff y floating IP donde los backdoor draws añaden equity marginal significativa.

**Solución:** Usar constantes calibradas: backdoor flush = 1.5, backdoor straight = 1.0 (con variación por gaps).

---

## L2: Flush Draw Penalty Sin Ajuste por Blocker

**Archivo:** `DangerPenaltyCalculator.cs:61-71`
**Severidad:** Baja
**Impacto:** Penaliza igual tenga hero blocker de flush o no

**El problema:**
```csharp
if (!boardChange.FlushCompleted && boardChange.FlushDrawAppeared)
{
    double flushDrawPenalty = rawEquity * 0.08 * streetDangerMultiplier;
    if (heroHandRank >= HandRank.TwoPair)
        flushDrawPenalty *= 0.5;
    else if (heroHandRank == HandRank.OnePair)
        flushDrawPenalty *= 0.75;
    penalty += flushDrawPenalty;
}
```

El código aplica reducción por hand rank (TwoPair → ×0.5, OnePair → ×0.75) pero **no por blocker**. Esto es asimétrico: los flush **completados** sí tienen blocker adjustment granular (nut ×0.35, non-nut ×0.55, board4flush ×0.7) en líneas 81-93, pero los flush **draws** no.

**Consecuencia:** Hero con As de la suit peligrosa recibe la misma penalización que hero sin ningún blocker. Esto lleva a folds excesivos en boards con flush draw cuando hero tiene el nut blocker.

**Solución:** Aplicar blocker reduction al flush draw penalty usando los mismos parámetros existentes del blocker effect.

---

## L3: Danger Penalties Ignoran Mejora del Hero

**Archivo:** `DangerPenaltyCalculator.cs:1-97`
**Severidad:** Baja
**Impacto:** Penaliza cuando hero completó el draw (flush/straight)

**El problema:**
Las penalties de FlushCompleted (equity × 35%) y StraightCompleted (equity × 18%) se aplican basándose únicamente en el estado del board, sin verificar si **hero es quien se beneficia** de la completación.

**Ejemplo:** Hero tiene As-Ks, board muestra 3s-7s-Jh-2s. El flush se completa con la carta del turn para el hero. DangerPenaltyCalculator aplica penalty de `equity × 35%` como si el flush completado fuera peligroso para hero, cuando en realidad hero tiene el nut flush.

**Contexto:** Actualmente `heroBlocksDangerSuit` reduce el penalty total (×0.35 con nut blocker), pero "bloquear" no es lo mismo que "completar". Hero puede tener 2 cartas del suit (flush hecho) y aún recibir ~12% de penalización (35% × 0.35).

**Solución:** Añadir parámetro `heroCompletedDraw` (bool) al calculador. Si hero completó flush o straight → skip la penalty correspondiente.

---

## L4: AF Cliff Cuando passive=0

**Archivo:** `OpponentProfile.cs:43-52`
**Severidad:** Baja
**Impacto:** Villano con 1 bet y 0 calls se clasifica como AF=3.0 (ultra-agresivo)

**El problema:**
```csharp
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
```

Mismo patrón en `AggressionFactorIP` y `AggressionFactorOOP`. Cuando `passive == 0`:
- 1 bet, 0 calls → AF = 3.0 (como un LAG extremo)
- 2 bets, 0 calls → AF = 3.0 (mismo valor)
- 10 bets, 0 calls → AF = 3.0 (¡mismo valor!)

El cap a 3.0 evita división por cero pero crea una discontinuidad: un villano con 1 bet y 1 call tiene AF = 1.0, mientras que con 1 bet y 0 calls salta a AF = 3.0. Esto afecta a `GetTypeForPosition()` donde el threshold es 1.5 para clasificar como agresivo.

**Consecuencia:** Villanos con sample size mínima (1-2 acciones sin calls) se clasifican automáticamente como agresivos, afectando las decisiones de bluff catch, randomización, y fold equity.

**Solución:** Usar fórmula suavizada: `AF = (aggressive + 1) / (passive + 1)` (Laplace smoothing), o cap proporcional `min(aggressive, 3.0)`.

---

## L5: Combo Draw Bonus Sin Ajuste por Board Texture

**Archivo:** `PostflopDecisionService.cs:162-165`
**Severidad:** Baja
**Impacto:** Sobrevalora combo draws en boards monotone/wet

**El problema:**
```csharp
if (hasComboDraw && street != BoardPosition.River && heroHandRank < HandRank.Straight)
    effectiveEquity += _profile.ComboDrawEquityBonus; // +6.0 fijo
```

El bonus de +6% de equity se aplica uniformemente sin considerar la textura del board:
- **Board Dry** (ej. Ks-7h-2d): Combo draw tiene alta equity implícita (villanos no se protegen) → +6 es adecuado o bajo
- **Board Wet/Monotone** (ej. 8s-9s-Ts): Combo draw compite con draws de otros jugadores → +6 sobrevalora porque hay reverse implied odds si villain tiene el mismo draw pero mejor

**Consecuencia:** En boards muy coordinados, hero sobrevalora sus draws y juega demasiado agresivamente con combo draws que pueden estar dominados.

**Solución:** Multiplicar el bonus por un factor de textura: Dry ×1.2, SemiDry ×1.0, SemiWet ×0.8, Wet ×0.6, Monotone ×0.5.

---

## L6: Multiway OOP ×0.5 Arbitrario

**Archivo:** `PostflopDecisionService.cs:251-262`
**Severidad:** Baja
**Impacto:** Penalty cuadrático OOP potencialmente excesivo o insuficiente

**El problema:**
```csharp
if (isInPosition)
{
    multiwayFoldPenalty = extraOpponents * PokerConstants.MultiwayFoldBelowIP;     // lineal
    multiwayValuePenalty = extraOpponents * PokerConstants.MultiwayThinValueIP;
}
else
{
    // OOP: cuadrático con factor ×0.5 arbitrario
    multiwayFoldPenalty = extraOpponents * extraOpponents * PokerConstants.MultiwayFoldBelowOOP * 0.5;
    multiwayValuePenalty = extraOpponents * extraOpponents * PokerConstants.MultiwayThinValueOOP * 0.5;
}
```

El factor `0.5` en OOP no tiene fundamentación. Análisis por número de oponentes extra:

| Extra Opp | IP Fold Penalty | OOP Fold Penalty | Ratio OOP/IP |
|-----------|----------------|-----------------|-------------|
| 1 | 2.0 | 3.0 (1×1×6×0.5) | 1.5× |
| 2 | 4.0 | 12.0 (4×6×0.5) | 3.0× |
| 3 | 6.0 | 27.0 (9×6×0.5) | 4.5× |

Con 3 oponentes extra OOP, `adjustedFoldBelow` sube +27 puntos (×streetMult), lo que prácticamente fuerza fold con cualquier equity < 70%. Esto puede ser correcto conceptualmente pero el factor 0.5 es arbitrario.

**Consecuencia:** En pots multiway 4+ jugadores OOP, el penalty cuadrático puede sobrepenalizar forzando folds con manos que tienen equity suficiente, o subpenalizar en spots 3-way donde debería ser más conservador.

**Solución:** Extraer el factor 0.5 a `StrategyProfile.MultiwayOOPQuadraticDamping` para poder calibrarlo empíricamente. Validar con backtest A/B.

---

## Alcance de Este Cambio

Los 6 refinamientos son independientes entre sí y pueden implementarse en cualquier orden. L2 y L3 comparten el mismo archivo (`DangerPenaltyCalculator.cs`) y pueden combinarse en un solo PR.

## Impacto Esperado

- **L1:** +1-2% equity accuracy en flop con backdoor draws
- **L2:** Menos folds incorrectos con nut blocker en flush draw boards
- **L3:** Eliminación de penalizaciones absurdas cuando hero tiene la mejor mano
- **L4:** Clasificación más fiable de villanos con pocas manos
- **L5:** Sizing más conservador con combo draws en boards peligrosos
- **L6:** Calibración empírica del penalty multiway OOP

**Estimación BB/100:** +0.5 a +1.5 BB/100 combinado (spots marginales que actualmente se juegan incorrectamente).
