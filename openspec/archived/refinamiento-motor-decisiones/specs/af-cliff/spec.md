# Spec L4: AF Suavizado con Laplace Smoothing

## Descripcion

`OpponentProfile.AggressionFactor` (y variantes IP/OOP) tiene un cliff cuando `passive == 0`: retorna 3.0 si hay acciones agresivas, 1.0 si no hay ninguna. Esto crea una discontinuidad donde 1 bet sin calls clasifica al villano como ultra-agresivo (AF=3.0), idéntico a un villano con 30 bets y 10 calls.

## Ubicacion

`src/OpenScrape.Domain/Entities/OpponentProfile.cs`, lineas 43-78

## Codigo Actual

```csharp
// Lineas 43-52: AF global
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

// Lineas 57-65: AF IP
public double AggressionFactorIP
{
    get
    {
        if (TimesAggressiveIP + TimesPassiveIP < 5) return -1;
        if (TimesPassiveIP == 0) return TimesAggressiveIP > 0 ? 3.0 : 1.0;
        return (double)TimesAggressiveIP / TimesPassiveIP;
    }
}

// Lineas 70-78: AF OOP
public double AggressionFactorOOP
{
    get
    {
        if (TimesAggressiveOOP + TimesPassiveOOP < 5) return -1;
        if (TimesPassiveOOP == 0) return TimesAggressiveOOP > 0 ? 3.0 : 1.0;
        return (double)TimesAggressiveOOP / TimesPassiveOOP;
    }
}
```

## Analisis de la Discontinuidad

Tabla de AF actual vs propuesto para sample sizes bajas:

| Aggressive | Passive | AF Actual | AF Laplace (a+1)/(p+1) |
|-----------|---------|-----------|------------------------|
| 0 | 0 | 1.0 | 0.50 → cap 1.0 |
| 1 | 0 | 3.0 | 1.00 |
| 2 | 0 | 3.0 | 1.50 |
| 3 | 0 | 3.0 | 2.00 |
| 1 | 1 | 1.0 | 1.00 |
| 3 | 1 | 3.0 | 2.00 |
| 10 | 5 | 2.0 | 1.83 |
| 30 | 10 | 3.0 | 2.82 |

Con Laplace smoothing `(aggressive + 1) / (passive + 1)`:
- Converge al AF real con sample size grande
- Evita division por cero naturalmente
- Regresion hacia 1.0 (neutral) con pocas manos → menos clasificaciones erroneas

**Impacto en GetTypeForPosition** (threshold: AF > 1.5 = agresivo):
- Actual: 1 bet, 0 calls → AF 3.0 → agresivo (INCORRECTO con 1 muestra)
- Propuesto: 1 bet, 0 calls → AF 1.0 → pasivo (CORRECTO: no hay evidencia suficiente)
- Propuesto: 3 bets, 0 calls → AF 2.0 → agresivo (CORRECTO: tendencia clara)

## Fix Propuesto

```csharp
public double AggressionFactor
{
    get
    {
        int aggressive = TimesPostflopBet + TimesPostflopRaised;
        int passive = TimesPostflopCalled;
        // Laplace smoothing: regresion a AF=1.0 con pocas muestras
        return (double)(aggressive + 1) / (passive + 1);
    }
}

public double AggressionFactorIP
{
    get
    {
        if (TimesAggressiveIP + TimesPassiveIP < 5) return -1;
        return (double)(TimesAggressiveIP + 1) / (TimesPassiveIP + 1);
    }
}

public double AggressionFactorOOP
{
    get
    {
        if (TimesAggressiveOOP + TimesPassiveOOP < 5) return -1;
        return (double)(TimesAggressiveOOP + 1) / (TimesPassiveOOP + 1);
    }
}
```

**Nota:** El threshold `-1` para sample size insuficiente en IP/OOP se mantiene (ya existente, lineas 59/72). Solo cambia el calculo cuando hay datos suficientes.

## Escenarios BDD

### Escenario 1: AF global con passive=0 y 1 accion agresiva
```
Dado villain con TimesPostflopBet=1, TimesPostflopRaised=0, TimesPostflopCalled=0
Cuando se consulta AggressionFactor
Entonces AF = (1+1)/(0+1) = 2.0/1.0 = 1.0
Y GetTypeForPosition clasifica como NO agresivo (AF 1.0 < 1.5)
```

### Escenario 2: AF global con passive=0 y multiples acciones agresivas
```
Dado villain con TimesPostflopBet=3, TimesPostflopRaised=1, TimesPostflopCalled=0
Cuando se consulta AggressionFactor
Entonces AF = (4+1)/(0+1) = 5.0/1.0 = 5.0
Y GetTypeForPosition clasifica como agresivo (AF 5.0 > 1.5)
```

### Escenario 3: AF global sin acciones
```
Dado villain con TimesPostflopBet=0, TimesPostflopRaised=0, TimesPostflopCalled=0
Cuando se consulta AggressionFactor
Entonces AF = (0+1)/(0+1) = 1.0
Y clasificacion neutral
```

### Escenario 4: AF converge al real con sample size grande
```
Dado villain con TimesPostflopBet=25, TimesPostflopRaised=5, TimesPostflopCalled=10
Cuando se consulta AggressionFactor
Entonces AF = (30+1)/(10+1) = 31/11 ≈ 2.82
Y AF real sin smoothing = 30/10 = 3.0
Y diferencia < 0.2 (convergencia)
```

### Escenario 5: AF IP con sample size insuficiente — retorna -1
```
Dado villain con TimesAggressiveIP=2, TimesPassiveIP=1 (total 3 < 5)
Cuando se consulta AggressionFactorIP
Entonces retorna -1 (sin cambios, logica existente)
```

### Escenario 6: AF IP con passive=0 y datos suficientes
```
Dado villain con TimesAggressiveIP=4, TimesPassiveIP=0 (total 4 + 0 = 4 < 5)
Cuando se consulta AggressionFactorIP
Entonces retorna -1 (sample size insuficiente)
```

### Escenario 7: AF IP con passive=0 y datos suficientes (5+)
```
Dado villain con TimesAggressiveIP=5, TimesPassiveIP=0 (total 5 >= 5)
Cuando se consulta AggressionFactorIP
Entonces AF = (5+1)/(0+1) = 6.0
Y agresivo: AF > 1.5
```

### Escenario 8: Transicion suave alrededor del threshold
```
Dado villain con TimesPostflopBet=1, TimesPostflopRaised=0, TimesPostflopCalled=1
Cuando se consulta AggressionFactor
Entonces AF = (1+1)/(1+1) = 1.0 (pasivo, < 1.5)
Y incrementar a Bet=2, Called=1 → AF = (2+1)/(1+1) = 1.5 (borderline)
Y incrementar a Bet=3, Called=1 → AF = (3+1)/(1+1) = 2.0 (agresivo)
```

## Tests Requeridos

1. **Test_AF_Global_PassiveZero_OneAction_Returns1** — 1 bet, 0 calls → 1.0
2. **Test_AF_Global_PassiveZero_MultipleActions_ReturnsSmoothed** — 4 aggressive, 0 calls → 5.0
3. **Test_AF_Global_NoActions_Returns1** — todo 0 → 1.0
4. **Test_AF_Global_ConvergesWithLargeSample** — 30 agg, 10 passive → ~2.82
5. **Test_AF_IP_InsufficientSample_ReturnsMinus1** — total < 5 → -1
6. **Test_AF_IP_PassiveZero_SufficientSample_ReturnsSmoothed** — 5 agg, 0 passive → 6.0
7. **Test_AF_OOP_PassiveZero_SufficientSample_ReturnsSmoothed** — misma logica OOP
8. **Test_GetTypeForPosition_LaplacePreventsEarlyAggressive** — 1 bet → AF 1.0 → no agresivo
9. **Test_GetTypeForPosition_FallbackToGlobalAF_Unchanged** — AF IP=-1 → usa AF global (regresion)
