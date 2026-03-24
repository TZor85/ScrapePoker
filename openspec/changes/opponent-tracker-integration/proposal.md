# Integración OpponentTracker en Game Loop + Optimización Monte Carlo

## Why

Dos mejoras independientes que combinadas representan el mayor salto de EV disponible:

### OpponentTracker (punto 1)

`OpponentTracker` está registrado como Singleton en `Program.cs`, tiene 8 métodos `Record*()` para capturar VPIP/PFR/AF/CBet/3Bet, clasifica automáticamente en TAG/LAG/TP/LP, y `PostflopDecisionService.DetermineAction` ya acepta `villainType` con 4 branches de ajuste implementados (LAG -5, TP +3, LP -4, TAG neutro). Pero **ningún `Record*()` se llama desde el game loop** y `villainType` siempre se pasa como `Unknown`. Toda la lógica de ajuste por oponente está muerta.

Conectar esto tiene impacto desproporcionado porque:
- Cada decisión postflop se beneficia (+5-10% ROI estimado)
- Ya está codificado — solo falta conectar los puntos
- `GetAdjustedFoldEquity` ajusta fold equity por tipo (LAG ×0.70, LP ×1.25)
- Con 20+ manos de cada villano, las decisiones se personalizan

### Rendimiento Monte Carlo (punto 3)

`MonteCarloSimulator` ya tiene optimizaciones (BitHandEvaluator, ThreadLocal buffers, Parallel.For), pero hay oportunidades de cache que reducirían latencia:
- El equity cache en `UnifiedPokerCalculator` tiene 256 entries max con key exacta — sin tolerancia para equities cercanas
- No hay cache de equity preflop por hand+situation (siempre recalcula MC cuando hay VillainRange)
- Iteraciones fijas (1000) incluso cuando la decisión es obvia (equity >75% o <25%)

## What Changes

### OpponentTracker
- Inyectar `OpponentTracker` en `FrmMain` constructor
- Capturar acciones del villano en el game loop: `RecordHandPlayed`, `RecordVPIP`, `RecordPFR`, `RecordPostflopAction`, `RecordCBetOpportunity`
- Pasar `villainType` real (no `Unknown`) a `DetermineAction` en las 3 llamadas (flop/turn/river)
- Pasar `adjustedFoldEquity` de `OpponentTracker.GetAdjustedFoldEquity()` en vez de fold equity base

### Rendimiento Monte Carlo
- Iteraciones adaptativas: equity clara (>75% o <25%) → 500 iteraciones, marginal → 1000
- Cache preflop: MC preflop con VillainRange cacheado por (hand, situation, numOpponents)
- Tolerancia en cache: si equity cacheada difiere <2% de la key → reutilizar

## Capabilities

### New Capabilities
- `opponent-profiling-live`: Captura acciones del villano en tiempo real y clasifica tipo.
- `adaptive-iterations`: Monte Carlo ajusta iteraciones según claridad de la decisión.
- `preflop-mc-cache`: Cache de equity Monte Carlo preflop con VillainRange.

### Modified Capabilities
- `decision-villain-type`: `DetermineAction` recibe tipo real del villano (no siempre Unknown).
- `fold-equity-adjusted`: Fold equity ajustada por perfil del oponente.
- `equity-cache`: Cache con tolerancia para reutilizar equities cercanas.

## Impact

- **`FrmMain.cs`** — Inyección OpponentTracker, calls Record*(), pasar villainType a DetermineAction
- **`UnifiedPokerCalculator.cs`** — Cache preflop, iteraciones adaptativas, tolerancia cache
- **`MonteCarloSimulator.cs`** — Parámetro de iteraciones dinámico (ya soporta `int? iterations`)
- **Sin nuevas dependencias externas.**
