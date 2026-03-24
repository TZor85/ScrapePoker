## MODIFIED Requirements

### Requirement: River debe pasar villainAggressorCheckedPreviousStreet a DetermineAction
La llamada a `PostflopDecisionService.DetermineAction()` desde el procesamiento de river en `FrmMain` SHALL incluir el parámetro `villainAggressorCheckedPreviousStreet` para que el path de probe bet (path 4 del PostflopDecisionService) pueda activarse en river.

#### Scenario: Villano agresor preflop checkeó turn, hero OOP en river sin bet
- **GIVEN** villano fue agresor preflop (hero no es agresor)
- **AND** villano NO apostó en turn (`VillainBetTurn == false`)
- **AND** hero está OOP en river
- **AND** no hay bet del villano en river (betSize == NoBet)
- **AND** equity >= ProbeBetMinEquity, no es multiway
- **WHEN** se llama a `DetermineAction` en river con `villainAggressorCheckedPreviousStreet: true`
- **THEN** el probe bet path se evalúa y puede retornar Bet 1/3 (probe)

#### Scenario: Villano apostó en turn (no checkeó)
- **GIVEN** villano apostó en turn (`VillainBetTurn == true`)
- **WHEN** se llama a `DetermineAction` en river
- **THEN** `villainAggressorCheckedPreviousStreet` es false
- **AND** el probe bet path NO se activa

#### Scenario: Hero fue agresor preflop
- **GIVEN** hero fue agresor preflop (isPreflopAggressor == true)
- **AND** villano no apostó en turn
- **WHEN** se llama a `DetermineAction` en river
- **THEN** `villainAggressorCheckedPreviousStreet` es false (hero es agresor, no el villano)
- **AND** el probe bet path NO se activa

#### Scenario: Valor del parámetro
- **GIVEN** la llamada a `DetermineAction` en river
- **WHEN** se construye el parámetro `villainAggressorCheckedPreviousStreet`
- **THEN** su valor es `!_postflopContext.VillainBetTurn && !riverIsAggressor`
- **AND** `riverIsAggressor` se determina con `PreflopAnalyzer.IsPreflopAggressor(effectiveSituation)`
