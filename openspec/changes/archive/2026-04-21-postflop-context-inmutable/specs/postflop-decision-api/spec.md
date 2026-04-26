## MODIFIED Requirements

### Requirement: Comportamiento funcional preservado tras el refactor

La refactorización de `PostflopGameContext` a record inmutable NO DEBE alterar las decisiones emitidas para cualquier entrada. Cada combinación de `(equity, street, situation, estado cross-street, ...)` que producía la acción X antes del refactor MUST seguir produciendo la acción X después, siempre que el holder contenga el mismo estado que antes tendría la clase mutable.

`PostflopDecisionService.DetermineAction(PostflopDecisionInput input)` MUST seguir aceptando los mismos campos de `PostflopDecisionInput` y produciendo el mismo `PostflopDecisionResult`. La única diferencia observable externamente es que cualquier intento de mutar el contexto dentro del servicio (no debería existir hoy, pero es sintácticamente posible) deja de compilar tras el cambio — lo cual es deseable.

#### Scenario: Suite de tests existente sigue verde

- **GIVEN** la suite de tests del solution en la rama base
- **WHEN** se ejecuta `dotnet test OpenScrape.sln` tras el refactor
- **THEN** todos los tests pasan
- **AND** ninguno de los tests que verifica `PostflopDecisionResult.Action` reporta una acción distinta a la esperada
- **AND** los tests de `DecisionMatrixIntegrationTests` mantienen idéntico el output para las 216 combinaciones cubiertas

#### Scenario: PostflopDecisionService no muta el contexto recibido

- **WHEN** se buscan asignaciones `.HeroBetFlop =`, `.VillainBetFlop =`, `.IsAnyoneAllIn =`, etc., en `src/OpenScrape.DecisionMaker/`
- **THEN** cero coincidencias (el servicio sólo lee del contexto)
