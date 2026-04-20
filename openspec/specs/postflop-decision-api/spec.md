## ADDED Requirements

### Requirement: IPostflopDecisionService expone un único overload de DetermineAction

La interfaz `IPostflopDecisionService` SHALL exponer exactamente **un** método `DetermineAction` que acepta un `PostflopDecisionInput` como único parámetro y retorna un `PostflopDecisionResult`. La interfaz MUST NOT declarar overloads adicionales con parámetros posicionales, ni métodos marcados `[Obsolete]` para `DetermineAction`.

#### Scenario: Contrato de la interfaz tras el refactor

- **WHEN** se inspeccionan los miembros públicos de `IPostflopDecisionService` por reflection
- **THEN** existe exactamente un método de nombre `DetermineAction`
- **AND** su firma es `PostflopDecisionResult DetermineAction(PostflopDecisionInput input)`
- **AND** ningún método de la interfaz está decorado con `[ObsoleteAttribute]`

#### Scenario: Compilación sin warnings CS0618 en el solution

- **WHEN** se ejecuta `dotnet build OpenScrape.sln`
- **THEN** la salida no contiene warnings `CS0618` relativos a `DetermineAction`
- **AND** el contador global de warnings CS0618 en el solution es cero

### Requirement: La lógica real de decisión vive en el método que acepta PostflopDecisionInput

El cuerpo completo del algoritmo de decisión postflop SHALL residir en `PostflopDecisionService.DetermineAction(PostflopDecisionInput input)`. NO debe existir un método interno que tome la lista expandida de parámetros posicionales al que este delegue. El `#pragma warning disable CS0618` y su `restore` correspondiente MUST eliminarse del archivo.

#### Scenario: No queda delegación legacy

- **WHEN** se inspecciona `PostflopDecisionService.cs`
- **THEN** el archivo no contiene la directiva `#pragma warning disable CS0618`
- **AND** el único método de nombre `DetermineAction` tiene la firma `PostflopDecisionResult DetermineAction(PostflopDecisionInput input)`

### Requirement: Comportamiento funcional preservado tras el refactor

La refactorización NO DEBE alterar las decisiones emitidas para cualquier entrada. Cada combinación de `(equity, street, situation, ...)` que producía la acción X antes del refactor MUST seguir produciendo la acción X después.

#### Scenario: Suite de tests existente sigue verde

- **GIVEN** la suite de 849 tests del solution en la rama base
- **WHEN** se ejecuta `dotnet test OpenScrape.sln` tras el refactor
- **THEN** 849 tests pasan
- **AND** ninguno de los tests que verifica `PostflopDecisionResult.Action` reporta una acción distinta a la esperada

#### Scenario: StrategyBacktester sigue reproduciendo decisiones históricas

- **GIVEN** un `HandRecord` histórico con `List<StreetDecision>` conocidas
- **WHEN** `StrategyBacktester.ReplayDecision` se invoca sobre cada street decision tras el refactor
- **THEN** las acciones simplificadas (`Bet/Call/Raise/Check/Fold/All-In`) coinciden con las emitidas antes del refactor
- **AND** no se lanza excepción por campos faltantes en el nuevo `PostflopDecisionInput`

### Requirement: Helper de construcción de PostflopDecisionInput para tests

Los tests unitarios que antes pasaban parámetros posicionales SHALL migrar a construir `PostflopDecisionInput` mediante un helper compartido `MakeInput(...)` (método estático privado por test fixture cuando aplique), o mediante inicializadores de objeto explícitos. El helper MUST exponer solo los parámetros que el test modifica; el resto usa los defaults del record.

#### Scenario: Un test que antes tenía 15 parámetros posicionales no los repite

- **GIVEN** un test que antes invocaba `DetermineAction(equity: 50, street: Flop, situation: OpenRaise, ...15 params nombrados...)`
- **WHEN** se migra al nuevo overload
- **THEN** el test invoca `DetermineAction(MakeInput(equity: 50, street: Flop, situation: OpenRaise, ...))` o `DetermineAction(new PostflopDecisionInput { Equity = 50, Street = Flop, Situation = OpenRaise, ... })`
- **AND** el test no referencia ningún miembro `[Obsolete]`
