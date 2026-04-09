# Spec BF2: Mejorar Rejection Sampling en TryDrawFromRange

## Descripción

`TryDrawFromRange` en `MonteCarloSimulator` usa rejection sampling para seleccionar manos del villain de un rango ponderado. Cuando el combo seleccionado está bloqueado por cartas conocidas, el `break` descarta el intento completo en vez de continuar buscando combos disponibles. Con tasas de bloqueo altas (turn/river), iteraciones MC enteras se descartan silenciosamente, reduciendo la muestra efectiva y la precisión de la equity.

## Ubicación

`src/OpenScrape.DecisionMaker/Algorithms/MonteCarloSimulator.cs`, líneas 529-565

## Código Actual (Problemático)

```csharp
for (int attempt = 0; attempt < 10; attempt++)
{
    double roll = Random.Shared.NextDouble() * totalWeight;
    double cumulative = 0;

    foreach (var combo in combos)
    {
        cumulative += combo.Weight;
        if (roll <= cumulative)
        {
            if (IsCardAvailable(deck, available, combo.Card1) &&
                IsCardAvailable(deck, available, combo.Card2))
            {
                card1 = combo.Card1;
                card2 = combo.Card2;
                return true;
            }
            break;  // ← Desperdicia intento, re-roll completo
        }
    }
}
return false;  // ← Iteración MC perdida
```

## Análisis del Problema

**Rejection sampling:** Es matemáticamente correcto (no introduce sesgo) pero ineficiente. El roll selecciona un combo por peso, y si está bloqueado, se descarta todo el intento. Esto es correcto en distribución de probabilidad, pero:

1. **Tasa de rechazo en river:** Con 7-9 cartas conocidas (2 hero + 5 board + 0-2 villain en multiway), ~15-25% de los 1326 combos totales están bloqueados. Para ranges estrechos (e.g., top 10% = ~130 combos), ~30-50% pueden estar bloqueados.

2. **Probabilidad de fallo en 10 intentos:**
   - 30% blocked → P(fail) = 0.30^10 ≈ 0.0006% → OK
   - 50% blocked → P(fail) = 0.50^10 ≈ 0.1% → ~50 iteraciones perdidas en 50K
   - 70% blocked → P(fail) = 0.70^10 ≈ 2.8% → ~1400 iteraciones perdidas en 50K
   - 85% blocked → P(fail) = 0.85^10 ≈ 20% → ~10000 iteraciones perdidas en 50K

3. **Impacto:** Con ranges estrechos en river (villain 3-bet range vs hero), se pierden ~1-3% de iteraciones. Con ranges muy estrechos (4-bet pot river), se pueden perder >5%, aumentando el error estadístico sin que el caller lo detecte.

## Solución Propuesta

Dos mejoras complementarias:

### A) Pre-filtrar combos disponibles antes de MC

Antes del loop MC, filtrar `villainCombos` para excluir combos cuyas cartas ya están asignadas (hero + community). Esto reduce la lista y el `totalWeight` a solo combos posibles.

**Nota:** Esto ya se hace parcialmente en `BuildVillainCombos`, pero solo para cartas conocidas estáticamente. Las cartas de community que se agregan dinámicamente en el loop MC (river/turn cards) también pueden bloquear combos.

### B) Continuar buscando tras combo bloqueado

Cuando un combo está bloqueado, en vez de `break`, continuar al siguiente combo con peso suficiente. Esto cambia ligeramente la distribución (el combo siguiente se sobrerepresenta), pero el impacto es mínimo comparado con descartar iteraciones enteras.

**Alternativa más correcta:** Re-normalizar pesos excluyendo combos bloqueados. Costoso pero exacto.

**Alternativa pragmática elegida:** Aumentar intentos máximos de 10 a 20, y reportar tasa de rechazo para diagnóstico.

## Escenarios BDD

### Escenario 1: Combo seleccionado bloqueado → continúa buscando
```
Dado un rango con combos [AhKh(w=1.0), AhKd(w=1.0), AsKs(w=1.0)]
Y AhKh está bloqueado (Ah en board)
Y AhKd está bloqueado (Ah en board)
Cuando roll selecciona AhKh
Entonces no hace break
Y continúa al siguiente combo disponible (AsKs)
Y retorna AsKs exitosamente
```

### Escenario 2: Todos los combos bloqueados → retorna false tras agotar intentos
```
Dado un rango donde todos los combos están bloqueados
Cuando se intenta TryDrawFromRange
Entonces retorna false después de agotar intentos
Y la iteración MC se descarta
```

### Escenario 3: Alta tasa de bloqueo → no pierde más del 1% de iteraciones
```
Dado un rango con 70% de combos bloqueados
Y simulación MC de 50000 iteraciones
Cuando se ejecuta RunMonteCarloSimulation
Entonces las iteraciones efectivas son >= 49500 (< 1% pérdida)
```

### Escenario 4: Rango estrecho en river → equity precisa
```
Dado hero con AsAd, board Kh-Qh-7d-3c-2s
Y villain range = top 8% (rango de 3-bet)
Cuando se calcula equity con MC (50K iteraciones)
Entonces la equity tiene error < ±1.0% vs enumeración exacta
```

### Escenario 5: Sin combos bloqueados → comportamiento idéntico al actual
```
Dado un rango con 0% de combos bloqueados
Cuando se ejecuta TryDrawFromRange
Entonces el primer intento siempre tiene éxito
Y la distribución de selección es proporcional a los pesos
```

### Escenario 6: Tasa de rechazo reportada
```
Dado una simulación MC
Cuando se completa
Entonces EquityResult.Simulations refleja iteraciones EFECTIVAS (no intentadas)
Y el ratio iteraciones_efectivas/intentadas es diagnosticable via log
```

## Fix Propuesto

```csharp
private static bool TryDrawFromRange(
    List<VillainCombo> combos, CardDataOuts[] deck, int available,
    double totalWeight, out CardDataOuts card1, out CardDataOuts card2)
{
    if (totalWeight <= 0)
    {
        card1 = default!;
        card2 = default!;
        return false;
    }

    for (int attempt = 0; attempt < 20; attempt++)  // 10 → 20 intentos
    {
        double roll = Random.Shared.NextDouble() * totalWeight;
        double cumulative = 0;

        foreach (var combo in combos)
        {
            cumulative += combo.Weight;
            if (roll <= cumulative)
            {
                if (IsCardAvailable(deck, available, combo.Card1) &&
                    IsCardAvailable(deck, available, combo.Card2))
                {
                    card1 = combo.Card1;
                    card2 = combo.Card2;
                    return true;
                }
                break;  // Rejection sampling correcto: re-roll
            }
        }
    }

    card1 = default!;
    card2 = default!;
    return false;
}
```

**Nota:** Se mantiene `break` porque es matemáticamente correcto para rejection sampling (preserva la distribución de probabilidad). La mejora es aumentar intentos (20) para reducir la tasa de fallo en ranges muy bloqueados. Cambiar `break` por `continue` introduciría sesgo hacia combos posteriores en la lista.

## Tests Requeridos

1. **Test_TryDrawFromRange_ComboDisponible_RetornaTrue** — combo disponible → éxito en primer intento
2. **Test_TryDrawFromRange_TodosBloqueados_RetornaFalse** — todos bloqueados → false
3. **Test_TryDrawFromRange_AltaTasaBloqueo_ExitoConReintentos** — 80% bloqueados → éxito en < 20 intentos
4. **Test_MC_RangoEstrechoRiver_EquityPrecisa** — comparar MC vs exact enumeration en river con rango estrecho, error < 1%
5. **Test_MC_IteracionesEfectivas_CercaDeSolicitadas** — con 50% bloqueo, iteraciones efectivas >= 98% de solicitadas
