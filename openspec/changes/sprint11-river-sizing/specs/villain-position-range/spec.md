## MODIFIED Requirements

### Requirement: VillainRange ajustado por posición del villain
Cuando la posición del villain es conocida, las frecuencias del VillainRange SHALL escalarse: EP más estrecho, BTN más amplio.

#### Scenario: Villain abrió desde EP → rango más estrecho
- **GIVEN** handSituation es OpenRaise, villainPosition es Early
- **WHEN** GetForSituation se invoca con posición
- **THEN** frecuencias del rango se multiplican por 0.7 (rango más estrecho)
- **AND** equity de hero vs este rango es MENOR que vs rango sin posición

#### Scenario: Villain abrió desde BTN → rango más amplio
- **GIVEN** handSituation es OpenRaise, villainPosition es Button
- **WHEN** GetForSituation se invoca con posición
- **THEN** frecuencias del rango se multiplican por 1.3 (rango más amplio)

#### Scenario: Posición None → sin ajuste (rango default)
- **GIVEN** villainPosition es None
- **WHEN** GetForSituation se invoca
- **THEN** usa rango default sin modificar frecuencias
