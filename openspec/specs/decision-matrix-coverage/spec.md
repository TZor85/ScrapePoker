## ADDED Requirements

### Requirement: Matriz sistemática de tests parametrizados cubre los 108 nodos street × situation × posición × bet state

La suite SHALL incluir un test `[TestCaseSource]` llamado `Matrix_ProducesValidDecision` que recorra todas las combinaciones de:
- `BoardPosition ∈ {Flop, Turn, River}` (3 valores postflop)
- `HandSituation ∈ {OpenRaise, RaiseOverLimper, Call, ThreeBet, FourBet, Squeeze, VsSqueeze, DonkBet, DonkBetVsOpenRaise}` (9 valores con threshold configurado en `appsettings.json`)
- `IsInPosition ∈ {true, false}` (2 valores)
- `VillainBetSize ∈ {NoBet, Medium}` (2 valores)
- `Equity bucket ∈ {Low=20.0, High=80.0}` (2 valores; Mid=50.0 opcional como extensión)

El producto da **216 casos** (3 × 9 × 2 × 2 × 2). El test MUST ejecutarlos todos sin fallo (salvo que haya un bug real del motor, en cuyo caso el fallo señala la regresión).

#### Scenario: Todos los casos producen una acción no vacía

- **WHEN** el test parametrizado se ejecuta para los 216 casos
- **THEN** cada invocación de `DetermineAction` retorna un `PostflopDecisionResult` con `Action` no null y no vacío
- **AND** el token inicial de `Action` (antes del primer espacio) pertenece al conjunto `{"Bet", "Call", "Raise", "Check", "Fold", "All-In"}`

### Requirement: Propiedades direccionales invariantes

Cada caso de la matriz SHALL verificar las siguientes invariantes direccionales, que son independientes de los thresholds específicos configurados:

1. **Equity alta + no facing bet → nunca `Fold`**. Con `Equity = 80` y `VillainBetSize = NoBet`, el resultado NO debe empezar por `Fold` (trivial: sin apuesta que callear no cabe foldear, y con equity alta no cabe renunciar al pot).
2. **Equity alta + facing bet → nunca `Fold`**. Con `Equity = 80` y `VillainBetSize = Medium`, el resultado debe ser `Call` o `Raise` (o `All-In`). Foldear equity 80% facing Medium es siempre −EV.
3. **Equity muy baja + facing bet → nunca `Raise`**. Con `Equity = 20` y `VillainBetSize = Medium`, el resultado NO debe empezar por `Raise` (raise como value con 20% es −EV; raise como bluff requiere fold equity suficiente pero el test usa defaults que no generan fold equity masiva).
4. **Sin facing bet → nunca `Call`**. Con `VillainBetSize = NoBet`, el resultado NO debe ser `Call` (no hay nada que callear).

#### Scenario: Equity alta no-facing bet sin Fold

- **WHEN** se ejecuta el caso `(street=Flop, situation=OpenRaise, isInPosition=true, villainBetSize=NoBet, equity=80)`
- **THEN** la `Action` no empieza por `"Fold"`
- **AND** la `Action` empieza por `"Bet"` o `"Check"`

#### Scenario: Equity alta facing bet nunca foldea

- **WHEN** se ejecuta cualquier caso con `equity=80, villainBetSize=Medium`
- **THEN** la `Action` no empieza por `"Fold"`

#### Scenario: Equity baja facing bet no levanta

- **WHEN** se ejecuta cualquier caso con `equity=20, villainBetSize=Medium`
- **THEN** la `Action` no empieza por `"Raise"`

#### Scenario: Sin bet no hay Call

- **WHEN** se ejecuta cualquier caso con `villainBetSize=NoBet`
- **THEN** la `Action` no es exactamente `"Call"` ni empieza por `"Call "`

### Requirement: Las claves de thresholds usadas existen en el diccionario cargado

La suite SHALL verificar que las 27 combinaciones `{Street}_{Situation}` usadas por la matriz existen como claves explícitas en `_profile.Thresholds` (no por fallback genérico). Esto previene que un typo en `appsettings.json` (p.ej. `"Flop_Squeze"`) pase al merge silencioso.

#### Scenario: Cada clave de la matriz tiene entrada

- **GIVEN** la lista de 27 claves usadas por la matriz
- **WHEN** se inspecciona el diccionario `StrategyProfile.Thresholds` cargado desde `appsettings.json`
- **THEN** cada clave está presente como entrada explícita (no se resuelve al fallback)

### Requirement: Ejecución rápida y determinística

La matriz completa SHALL ejecutarse en menos de **5 segundos** en una máquina de desarrollo típica. Los tests MUST ser determinísticos: no pueden depender de MC con aleatoriedad (la equity se pasa directamente como parámetro del test, no se calcula con Monte Carlo).

#### Scenario: Ejecución bajo el threshold de tiempo

- **WHEN** se ejecuta `dotnet test --filter "FullyQualifiedName~DecisionMatrixIntegrationTests"`
- **THEN** la duración total es < 5 segundos
- **AND** dos ejecuciones consecutivas con el mismo código producen idénticos resultados (ningún caso oscila entre pasar y fallar)

### Requirement: Fallos señalan contexto mínimo reproducible

Cuando un caso de la matriz falla, el mensaje de error SHALL incluir:
- La combinación exacta: `Street`, `Situation`, `IsInPosition`, `VillainBetSize`, `Equity`
- La `Action` emitida y la propiedad invariante violada

#### Scenario: Fallo informativo

- **GIVEN** un caso hipotético que falla con `Action = "Fold"` cuando `equity=80, villainBetSize=NoBet`
- **WHEN** se lee el output del test
- **THEN** el mensaje identifica unívocamente `(Flop, OpenRaise, IP=true, NoBet, Eq=80)` y la propiedad `"Equity alta no-facing bet debe evitar Fold"`
