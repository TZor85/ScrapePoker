## ADDED Requirements

### Requirement: Aggression Factor separado por posición
OpponentProfile SHALL trackear acciones agresivas/pasivas IP y OOP por separado.

#### Scenario: Villain agresivo IP, pasivo OOP
- **GIVEN** villain con TimesAggressiveIP=8, TimesPassiveIP=2, TimesAggressiveOOP=2, TimesPassiveOOP=6
- **WHEN** se calcula AggressionFactorIP y AggressionFactorOOP
- **THEN** AFIP = 4.0 (muy agresivo), AFOOP = 0.33 (muy pasivo)

#### Scenario: Datos insuficientes → fallback a -1
- **GIVEN** villain con TimesAggressiveIP=2, TimesPassiveIP=1 (solo 3 acciones IP)
- **WHEN** se calcula AggressionFactorIP
- **THEN** retorna -1 (< 5 acciones mínimas)

### Requirement: GetTypeForPosition usa AF posicional
OpponentProfile SHALL exponer GetTypeForPosition(bool villainIsIP) que clasifica al oponente según AF en esa posición.

#### Scenario: LAG IP pero TP OOP
- **GIVEN** villain con VPIP=35, AFIP=3.0, AFOOP=0.8
- **WHEN** GetTypeForPosition(villainIsIP=true) y GetTypeForPosition(villainIsIP=false)
- **THEN** IP → LAG (loose + aggressive), OOP → LP (loose + passive)

#### Scenario: Sin datos posicionales → fallback a AF global
- **GIVEN** villain con AFIP=-1 (insuficiente), AF global=2.0
- **WHEN** GetTypeForPosition(villainIsIP=true)
- **THEN** usa AF global (2.0) para clasificar
