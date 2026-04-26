## ADDED Requirements

### Requirement: ThresholdKey es un value object tipado de la combinación (street, situación)

El dominio SHALL exponer un record `ThresholdKey(BoardPosition Street, HandSituation Situation)` en `OpenScrape.Domain.ValueObjects`. La construcción MUST rechazar combinaciones inválidas para lookup postflop: `Street ∈ {None, Hand}` y `Situation = None` lanzan `ArgumentException` en el constructor.

`HandSituation.Call` sí se considera combinación postflop válida — representa al jugador que llamó la apuesta preflop y ahora afronta el flop/turn/river, y el `appsettings.json` actual define `Flop_Call`, `Turn_Call`, `River_Call`.

`ThresholdKey` MUST implementar igualdad por valor (gratuito por ser `record`) y exponer un método `ToString()` que devuelva `"{Street}_{Situation}"` para compatibilidad con los logs existentes y con las claves crudas de `appsettings.json`.

#### Scenario: Construcción con combinación postflop válida

- **GIVEN** `street = BoardPosition.Turn` y `situation = HandSituation.OpenRaise`
- **WHEN** se construye `new ThresholdKey(street, situation)`
- **THEN** la instancia se crea sin excepción
- **AND** `key.ToString()` retorna `"Turn_OpenRaise"`

#### Scenario: Rechazo de street inválido

- **GIVEN** `street = BoardPosition.Hand` (sólo aplica preflop)
- **WHEN** se intenta `new ThresholdKey(street, HandSituation.OpenRaise)`
- **THEN** se lanza `ArgumentException`
- **AND** el mensaje incluye el nombre del parámetro `street` y el valor inválido

#### Scenario: Rechazo de situación None

- **GIVEN** `situation = HandSituation.None`
- **WHEN** se intenta `new ThresholdKey(BoardPosition.Flop, situation)`
- **THEN** se lanza `ArgumentException`
- **AND** el mensaje incluye el nombre del parámetro `situation` y el valor inválido

#### Scenario: Aceptación de situación Call

- **GIVEN** `situation = HandSituation.Call`
- **WHEN** se construye `new ThresholdKey(BoardPosition.Flop, situation)`
- **THEN** la instancia se crea sin excepción
- **AND** `key.ToString()` retorna `"Flop_Call"`

#### Scenario: Igualdad por valor

- **GIVEN** `a = new ThresholdKey(BoardPosition.River, HandSituation.Squeeze)` y `b = new ThresholdKey(BoardPosition.River, HandSituation.Squeeze)`
- **WHEN** se comparan con `a == b` y `a.GetHashCode() == b.GetHashCode()`
- **THEN** ambas comparaciones retornan `true`

### Requirement: IThresholdsRegistry expone lookup tipado sin fallback silencioso

El proyecto `OpenScrape.DecisionMaker` SHALL publicar la interfaz `IThresholdsRegistry` con la siguiente superficie:

- `StreetThresholds Get(ThresholdKey key)` — retorna la configuración correspondiente. MUST lanzar `KeyNotFoundException` si la clave no existe (el validador de arranque garantiza que esto no ocurre en runtime normal).
- `bool TryGet(ThresholdKey key, out StreetThresholds thresholds)` — variante no lanzadora para rutas opcionales.
- `bool Contains(ThresholdKey key)` — utilidad para el validador.
- `IReadOnlyCollection<ThresholdKey> Keys` — enumera las claves cargadas.

La implementación `ThresholdsRegistry` MUST registrarse como singleton y construirse una única vez desde `IOptions<StrategyProfile>`, parseando las claves string crudas a `ThresholdKey` al construir. Claves string que no parsean MUST provocar fallo en el constructor del registry (no fallback).

#### Scenario: Lookup exitoso

- **GIVEN** un `StrategyProfile.Thresholds` que contiene `"Flop_OpenRaise"` con `FoldBelow = 35`
- **AND** un `ThresholdsRegistry` construido a partir de ese perfil
- **WHEN** se llama `registry.Get(new ThresholdKey(BoardPosition.Flop, HandSituation.OpenRaise))`
- **THEN** retorna el `StreetThresholds` con `FoldBelow = 35`

#### Scenario: Lookup de clave ausente lanza

- **GIVEN** un `ThresholdsRegistry` sin entrada para `(River, DonkBet)`
- **WHEN** se llama `registry.Get(new ThresholdKey(BoardPosition.River, HandSituation.DonkBet))`
- **THEN** lanza `KeyNotFoundException`
- **AND** el mensaje incluye `"River_DonkBet"`

#### Scenario: TryGet retorna false sin lanzar

- **GIVEN** el mismo registry sin `(River, DonkBet)`
- **WHEN** se llama `registry.TryGet(new ThresholdKey(BoardPosition.River, HandSituation.DonkBet), out var t)`
- **THEN** retorna `false`
- **AND** `t` es el valor `default` de `StreetThresholds`
- **AND** no se lanza excepción

#### Scenario: Construcción con clave string inválida en el perfil

- **GIVEN** un `StrategyProfile.Thresholds` con una clave `"Flop_Inexistente"` (no parsea a `HandSituation`)
- **WHEN** se construye `new ThresholdsRegistry(Options.Create(profile))`
- **THEN** lanza `InvalidOperationException`
- **AND** el mensaje identifica la clave inválida `"Flop_Inexistente"`

### Requirement: Registro DI del IThresholdsRegistry

`OpenScrape.DecisionMaker.Services` SHALL registrar `IThresholdsRegistry` → `ThresholdsRegistry` como singleton dentro de `AddDecisionMaker(this IServiceCollection)`. La resolución de `IThresholdsRegistry` desde `IServiceProvider` MUST devolver siempre la misma instancia.

#### Scenario: Singleton resuelto por DI

- **GIVEN** un `IServiceProvider` construido tras `services.AddDecisionMaker()`
- **WHEN** se resuelve `IThresholdsRegistry` dos veces
- **THEN** ambas resoluciones retornan la misma instancia (referencia idéntica)
