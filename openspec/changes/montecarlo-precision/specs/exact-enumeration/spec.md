## ADDED Requirements

### Requirement: Enumeración exacta en river
Cuando hay 5 community cards conocidas, el sistema SHALL calcular la equity enumerando TODAS las combinaciones posibles de manos del oponente en lugar de sampling aleatorio.

#### Scenario: River sin rango — enumera C(45,2) = 990 manos
- **GIVEN** hero con 2 hole cards y 5 community cards (river completo)
- **WHEN** se calcula equity sin villain range
- **THEN** se evalúan las 990 combinaciones posibles de 2 cartas del oponente (C(45,2)) y la equity es determinística (sin varianza entre ejecuciones)

#### Scenario: River con rango — enumera combos ponderados
- **GIVEN** hero con 2 hole cards, 5 community cards y un villain range definido
- **WHEN** se calcula equity
- **THEN** se evalúan todos los combos del rango que no están bloqueados, ponderados por peso, y la equity es determinística

#### Scenario: Quads en river — equity >= 99%
- **GIVEN** hero con AA y board A A K 7 2
- **WHEN** se calcula equity exacta
- **THEN** la equity es >= 99% (solo pierde contra straight flush específico)

### Requirement: Enumeración exacta en turn
Cuando hay 4 community cards conocidas, el sistema SHALL calcular la equity enumerando todas las posibles river cards y manos del oponente.

#### Scenario: Turn sin rango — enumera ~42K combinaciones
- **GIVEN** hero con 2 hole cards y 4 community cards (turn)
- **WHEN** se calcula equity sin villain range
- **THEN** se enumeran las 45 posibles river cards × C(44,2) manos del oponente ≈ 42K evaluaciones

#### Scenario: Turn con rango — enumera river × combos
- **GIVEN** hero con 2 hole cards, 4 community cards y villain range
- **WHEN** se calcula equity
- **THEN** se enumeran las posibles river cards × combos válidos del rango ponderados

## MODIFIED Requirements

### Requirement: Flop/Preflop usa Monte Carlo con más iteraciones
Para 3 o menos community cards, el sistema SHALL usar simulación Monte Carlo con iteraciones adaptativas.

#### Scenario: Flop usa 50.000 iteraciones por defecto
- **GIVEN** hero con 2 hole cards y 3 community cards (flop)
- **WHEN** se calcula equity sin especificar iteraciones
- **THEN** se ejecutan 50.000 simulaciones MC

#### Scenario: Preflop usa 30.000 iteraciones por defecto
- **GIVEN** hero con 2 hole cards y 0 community cards (preflop)
- **WHEN** se calcula equity sin especificar iteraciones
- **THEN** se ejecutan 30.000 simulaciones MC

#### Scenario: Iteraciones personalizadas respetadas
- **GIVEN** un llamado con `iterations = 5000`
- **WHEN** se calcula equity en flop
- **THEN** se ejecutan exactamente 5.000 simulaciones (override adaptativo)
