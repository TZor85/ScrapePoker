## MODIFIED Requirements

### Requirement: Cálculo correcto de blinds
El sistema deberá calcular que P0 (Hero) es BigBlind cuando dealer es P3 y hay jugadores en P0, P1, P3.

#### Scenario: Dealer en P3 con 3 activos
- **WHEN** dealer=P3, activeSeats=[0,1,3]
- **THEN** distancia = (2-0+3)%3 = 2 = BigBlind