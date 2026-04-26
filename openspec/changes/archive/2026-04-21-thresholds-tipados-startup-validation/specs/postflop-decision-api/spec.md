## MODIFIED Requirements

### Requirement: IPostflopDecisionService expone un único overload de DetermineAction

La interfaz `IPostflopDecisionService` SHALL exponer exactamente **un** método `DetermineAction` que acepta un `PostflopDecisionInput` como único parámetro y retorna un `PostflopDecisionResult`. La interfaz MUST NOT declarar overloads adicionales con parámetros posicionales, ni métodos marcados `[Obsolete]` para `DetermineAction`.

Adicionalmente, `PostflopDecisionService` SHALL resolver sus thresholds exclusivamente a través de `IThresholdsRegistry` inyectado por DI. La clase NO DEBE exponer un método público `GetThresholds(BoardPosition, HandSituation)` con fallback a valores hardcoded: el fallback queda eliminado y toda ruta que consulte thresholds delega en el registry.

#### Scenario: Contrato de la interfaz tras el refactor

- **WHEN** se inspeccionan los miembros públicos de `IPostflopDecisionService` por reflection
- **THEN** existe exactamente un método de nombre `DetermineAction`
- **AND** su firma es `PostflopDecisionResult DetermineAction(PostflopDecisionInput input)`
- **AND** ningún método de la interfaz está decorado con `[ObsoleteAttribute]`

#### Scenario: Compilación sin warnings CS0618 en el solution

- **WHEN** se ejecuta `dotnet build OpenScrape.sln`
- **THEN** la salida no contiene warnings `CS0618` relativos a `DetermineAction`
- **AND** el contador global de warnings CS0618 en el solution es cero

#### Scenario: PostflopDecisionService ya no contiene GetThresholds con fallback

- **WHEN** se inspecciona `PostflopDecisionService.cs`
- **THEN** no existe un método público `GetThresholds(BoardPosition, HandSituation)` que construya un `StreetThresholds` inline con valores hardcoded (`FoldBelow = 40`, etc.)
- **AND** no existe la llamada `Console.WriteLine("[WARNING] Threshold no encontrado...")`
- **AND** el servicio depende de `IThresholdsRegistry` vía constructor

### Requirement: La lógica real de decisión vive en el método que acepta PostflopDecisionInput

El cuerpo completo del algoritmo de decisión postflop SHALL residir en `PostflopDecisionService.DetermineAction(PostflopDecisionInput input)`. NO debe existir un método interno que tome la lista expandida de parámetros posicionales al que este delegue. El `#pragma warning disable CS0618` y su `restore` correspondiente MUST eliminarse del archivo.

Las consultas internas de thresholds dentro de `DetermineAction` y helpers relacionados MUST sustituirse por `_thresholdsRegistry.Get(new ThresholdKey(street, situation))`.

#### Scenario: No queda delegación legacy

- **WHEN** se inspecciona `PostflopDecisionService.cs`
- **THEN** el archivo no contiene la directiva `#pragma warning disable CS0618`
- **AND** el único método de nombre `DetermineAction` tiene la firma `PostflopDecisionResult DetermineAction(PostflopDecisionInput input)`

#### Scenario: Los accesos internos a thresholds van por el registry

- **WHEN** se buscan cadenas literales `$"{street}_{situation}"` o equivalentes en `PostflopDecisionService.cs`
- **THEN** no se encuentran
- **AND** todas las consultas de thresholds usan `_thresholdsRegistry.Get(...)` o `_thresholdsRegistry.TryGet(...)`

### Requirement: Comportamiento funcional preservado tras el refactor

La refactorización NO DEBE alterar las decisiones emitidas para cualquier entrada. Cada combinación de `(equity, street, situation, ...)` que producía la acción X antes del refactor MUST seguir produciendo la acción X después, siempre que la configuración cargada sea la misma.

#### Scenario: Suite de tests existente sigue verde

- **GIVEN** la suite de tests del solution en la rama base
- **WHEN** se ejecuta `dotnet test OpenScrape.sln` tras el refactor
- **THEN** todos los tests pasan
- **AND** ninguno de los tests que verifica `PostflopDecisionResult.Action` reporta una acción distinta a la esperada
