## MODIFIED Requirements

### Requirement: Penalización multiway escala con posición y nº de villanos detrás
La penalización por pot multiway SHALL escalar cuadráticamente para OOP y considerar cuántos villanos quedan por actuar detrás del hero.

#### Scenario: 3-way pot IP — penalización moderada
- **GIVEN** hero IP en pot 3-way (2 oponentes, ambos ya actuaron)
- **WHEN** se calcula ajuste multiway
- **THEN** `adjustedFoldBelow += 1 × MultiwayFoldBelowIP` (penalización lineal, como antes)

#### Scenario: 3-way pot OOP con 2 villanos detrás — penalización exponencial
- **GIVEN** hero OOP en pot 3-way con 2 villanos aún por actuar detrás
- **WHEN** se calcula ajuste multiway
- **THEN** `adjustedFoldBelow += extraOpponents × MultiwayFoldBelowOOP × 1.5` (factor 1.5× OOP con villanos detrás)

#### Scenario: 4-way pot OOP — penalización cuadrática
- **GIVEN** hero OOP en pot 4-way (3 oponentes)
- **WHEN** se calcula ajuste multiway con `extraOpponents = 3`
- **THEN** la penalización escala como `extraOpponents² × MultiwayFoldBelowOOP × 0.5` (cuadrática) en vez de lineal

#### Scenario: Turn/River multiway más peligroso que flop
- **GIVEN** pot multiway en turn o river
- **WHEN** se calcula ajuste multiway
- **THEN** se aplica multiplicador de street: flop ×1.0, turn ×1.2, river ×1.4 (ranges más estrechas en streets tardías)

#### Scenario: 2-way pot (heads-up) — sin penalización
- **GIVEN** hero en pot heads-up (`numOpponents == 1`)
- **WHEN** se evalúa multiway
- **THEN** no se aplica ninguna penalización multiway (comportamiento actual preservado)
