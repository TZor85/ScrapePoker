## ADDED Requirements

### Requirement: Validador de StrategyProfile ejecuta al arrancar el host

La aplicación SHALL ejecutar `StrategyProfileValidator.Validate(StrategyProfile profile)` durante el arranque del host, antes de mostrar la ventana principal y antes de ejecutar el game loop. El validador MUST ejecutarse exactamente una vez por proceso y MUST abortar el arranque con un mensaje explícito si detecta cualquier error.

La integración con el host se realiza en `Program.cs` tras `builder.Build()`, resolviendo `IOptions<StrategyProfile>` y pasando el valor al validador. Si el validador lanza, el proceso MUST terminar con código de salida distinto de cero.

#### Scenario: Arranque con configuración válida

- **GIVEN** un `appsettings.json` con todas las claves `Flop_*`, `Turn_*`, `River_*` esperadas y tiers coherentes
- **WHEN** arranca la aplicación
- **THEN** `StrategyProfileValidator.Validate` no lanza
- **AND** el formulario `FrmMain` se muestra

#### Scenario: Arranque con clave faltante aborta

- **GIVEN** un `appsettings.json` al que le falta la entrada `"Flop_OpenRaise"`
- **WHEN** arranca la aplicación
- **THEN** `StrategyProfileValidator.Validate` lanza `StrategyProfileValidationException`
- **AND** el mensaje lista explícitamente `"Flop_OpenRaise"` como clave ausente
- **AND** el proceso termina con código de salida distinto de cero
- **AND** el formulario principal NO se muestra

### Requirement: Validación de claves string del StrategyProfile

El validador SHALL comprobar que toda clave del diccionario `StrategyProfile.Thresholds` parsea exactamente al formato `"{BoardPosition}_{HandSituation}"` donde:

- `BoardPosition` es uno de `Flop`, `Turn`, `River` (no `None`, no `Hand`, no cualquier otro valor).
- `HandSituation` es un valor válido del enum distinto de `None` (incluye `Call`, que es una situación postflop legítima).

Claves con formato incorrecto (sin guión bajo, con múltiples guiones bajos, con valores desconocidos) MUST añadirse a la lista de errores.

#### Scenario: Clave con street inválido

- **GIVEN** `StrategyProfile.Thresholds` contiene `"Preflop_OpenRaise"`
- **WHEN** se ejecuta el validador
- **THEN** lanza `StrategyProfileValidationException`
- **AND** el mensaje indica que `"Preflop"` no es un valor válido de `BoardPosition` para thresholds postflop

#### Scenario: Clave con formato malformado

- **GIVEN** `StrategyProfile.Thresholds` contiene `"FlopOpenRaise"` (sin guión bajo)
- **WHEN** se ejecuta el validador
- **THEN** lanza `StrategyProfileValidationException`
- **AND** el mensaje indica que la clave no respeta el formato `"{Street}_{Situation}"`

#### Scenario: Clave con situación desconocida

- **GIVEN** `StrategyProfile.Thresholds` contiene `"Turn_Foobar"`
- **WHEN** se ejecuta el validador
- **THEN** lanza `StrategyProfileValidationException`
- **AND** el mensaje identifica `"Foobar"` como `HandSituation` desconocido

### Requirement: Cobertura obligatoria de combinaciones postflop

El validador SHALL comprobar que existe una entrada en `StrategyProfile.Thresholds` para cada combinación `(Street, Situation)` declarada obligatoria por la estrategia. La lista obligatoria mínima MUST incluir todas las combinaciones que el `PostflopDecisionService` consulta en sus rutas de decisión activas, concretamente:

- Streets: `Flop`, `Turn`, `River`
- Situaciones (todas postflop): `OpenRaise`, `Call`, `RaiseOverLimper`, `ThreeBet`, `OpenRaiseVs3Bet`, `OpenRaiseVs3BetAndCall`, `FourBet`, `Cold4Bet`, `Squeeze`, `VsSqueeze`, `DonkBet`, `DonkBetVsOpenRaise`

Cada combinación faltante MUST aparecer en el mensaje de error acumulado (no detenerse en la primera).

#### Scenario: Múltiples combinaciones faltantes reportadas juntas

- **GIVEN** `StrategyProfile.Thresholds` sin `"River_DonkBet"` ni `"Turn_Squeeze"`
- **WHEN** se ejecuta el validador
- **THEN** lanza `StrategyProfileValidationException`
- **AND** el mensaje lista ambas claves faltantes
- **AND** no se detiene al encontrar la primera

### Requirement: Validación de coherencia de tiers

El validador SHALL comprobar que para cada `StreetThresholds` cargado se cumple la invariante monótona creciente:

`FoldBelow ≤ ThinValueAbove ≤ ValueAbove ≤ StrongValueAbove`

Y que todos los valores están en el rango `[0, 100]`. Violaciones MUST reportarse identificando la clave afectada y el tier inconsistente.

#### Scenario: Tiers invertidos

- **GIVEN** `"Flop_OpenRaise"` con `FoldBelow = 50, ThinValueAbove = 40`
- **WHEN** se ejecuta el validador
- **THEN** lanza `StrategyProfileValidationException`
- **AND** el mensaje identifica la clave `"Flop_OpenRaise"`
- **AND** el mensaje explica que `FoldBelow (50) > ThinValueAbove (40)`

#### Scenario: Tier fuera de rango

- **GIVEN** `"Turn_ThreeBet"` con `StrongValueAbove = 120`
- **WHEN** se ejecuta el validador
- **THEN** lanza `StrategyProfileValidationException`
- **AND** el mensaje identifica que `StrongValueAbove` está fuera de `[0, 100]`

### Requirement: Configuración actual de appsettings.json pasa la validación

La configuración committeada actualmente en `src/OpenScrape.App/appsettings.json` MUST pasar el validador sin errores tras aplicar este cambio. Se cubre con un test de integración que carga el archivo real vía `ConfigurationBuilder` y ejecuta el validador.

#### Scenario: appsettings.json real valida en verde

- **GIVEN** el `appsettings.json` del repositorio en `src/OpenScrape.App/`
- **WHEN** un test lo carga con `ConfigurationBuilder` y ejecuta `StrategyProfileValidator.Validate`
- **THEN** no se lanza ninguna excepción
