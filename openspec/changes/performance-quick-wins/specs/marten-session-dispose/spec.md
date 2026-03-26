## MODIFIED Requirements

### Requirement: Sesiones Marten con dispose correcto
Todas las sesiones Marten creadas en UseCases de cards SHALL usar `await using` para garantizar la liberación de recursos al finalizar el scope.

#### Scenario: GetCardsFlopUseCase dispone la sesión
- **GIVEN** que `GetCardsFlopUseCase.ExecuteAsync()` crea una sesión Marten
- **WHEN** el método termina (con éxito o excepción)
- **THEN** la sesión se libera automáticamente via `IAsyncDisposable.DisposeAsync()`

#### Scenario: GetCardsTurnUseCase dispone la sesión
- **GIVEN** que `GetCardsTurnUseCase.ExecuteAsync()` crea una sesión Marten
- **WHEN** el método termina (con éxito o excepción)
- **THEN** la sesión se libera automáticamente via `IAsyncDisposable.DisposeAsync()`

#### Scenario: GetCardsRiverUseCase dispone la sesión
- **GIVEN** que `GetCardsRiverUseCase.ExecuteAsync()` crea una sesión Marten
- **WHEN** el método termina (con éxito o excepción)
- **THEN** la sesión se libera automáticamente via `IAsyncDisposable.DisposeAsync()`

#### Scenario: Excepción no impide el dispose
- **GIVEN** que ocurre una excepción durante la ejecución del UseCase
- **WHEN** el stack se desenrolla
- **THEN** `await using` garantiza que `DisposeAsync()` se llama antes de propagar la excepción

#### Scenario: Conexiones de BD no se acumulan
- **GIVEN** que el game loop ejecuta 100 hands consecutivas
- **WHEN** cada hand usa GetCardsFlop + GetCardsTurn + GetCardsRiver
- **THEN** las conexiones de BD se liberan tras cada UseCase y no se acumulan en el pool
