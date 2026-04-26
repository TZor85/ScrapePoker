## MODIFIED Requirements

### Requirement: Cálculo de posición del Hero cuando faltan jugadores
El sistema deberá calcular correctamente la posición del Hero (P0) aunque otros asientos estén vacíos o en sit-out.

#### Scenario: Mesa con dealer y Hero activo
- **WHEN** el dealer está en el asiento P3, Hero está en P0, y faltan otros jugadores
- **THEN** el sistema calcula la distancia correcta desde el dealer y retorna la posición соответствую (Button, CutOff, etc.)

#### Scenario: P0 marcado como Empty
- **WHEN** P0 está marcado como Empty pero es el jugador local
- **THEN** el sistema incluye a P0 en el cálculo de posición