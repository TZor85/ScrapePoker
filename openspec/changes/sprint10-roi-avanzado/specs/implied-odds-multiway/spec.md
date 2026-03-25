## MODIFIED Requirements

### Requirement: Implied odds deben ajustarse por número de oponentes
`CalculateImpliedOddsFactor` SHALL recibir `numOpponents` y ajustar: OOP multiway peor, IP multiway con draw mejor.

#### Scenario: Implied odds heads-up — sin ajuste multiway
- **GIVEN** numOpponents es 1
- **WHEN** CalculateImpliedOddsFactor se invoca
- **THEN** no aplica ajuste multiway (factor sin cambio)

#### Scenario: Implied odds 3-way OOP — peores
- **GIVEN** numOpponents es 3, isInPosition es false
- **WHEN** CalculateImpliedOddsFactor se invoca
- **THEN** sprFactor se multiplica por 1.0 + 0.05×(3-1) = 1.10
- **AND** implied odds resultantes son PEORES (factor más alto = peor)

#### Scenario: Implied odds 3-way IP con flush draw — mejores
- **GIVEN** numOpponents es 3, isInPosition es true, hasFlushDraw es true
- **WHEN** CalculateImpliedOddsFactor se invoca
- **THEN** sprFactor se multiplica por 1.0 - 0.03×(3-1) = 0.94
- **AND** implied odds resultantes son MEJORES (más gente que pagar el flush)

#### Scenario: Implied odds 3-way IP sin draw — sin ajuste draw
- **GIVEN** numOpponents es 3, isInPosition es true, hasFlushDraw es false
- **WHEN** CalculateImpliedOddsFactor se invoca
- **THEN** no aplica bonus IP multiway draw (solo aplica con flush draw)

#### Scenario: Implied odds river — siempre 1.0 sin importar multiway
- **GIVEN** street es River, numOpponents es 3
- **WHEN** CalculateImpliedOddsFactor se invoca
- **THEN** retorna 1.0 (sin implied odds en river)
