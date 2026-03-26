## MODIFIED Requirements

### Requirement: Randomización adaptativa según villain type
La frecuencia de bet en la zona de randomización (equity en boundary de ThinValueAbove ± margin) SHALL ajustarse según el tipo de oponente.

#### Scenario: vs LAG → bet más (85%)
- **GIVEN** equity en boundary, villainType=LAG
- **WHEN** se evalúa randomización
- **THEN** bet frequency = 85% (LAG ajusta → explotar su call frequency, menos check)

#### Scenario: vs LP (fish) → bet más (80%)
- **GIVEN** equity en boundary, villainType=LP
- **WHEN** se evalúa randomización
- **THEN** bet frequency = 80% (fish paga → extraer valor directo)

#### Scenario: vs TP (nit) → check más (55% bet)
- **GIVEN** equity en boundary, villainType=TP
- **WHEN** se evalúa randomización
- **THEN** bet frequency = 55% (nit solo calla con mano fuerte → proteger rango de check)

#### Scenario: vs TAG (reg) → balance GTO (60% bet)
- **GIVEN** equity en boundary, villainType=TAG
- **WHEN** se evalúa randomización
- **THEN** bet frequency = 60% (reg explota desbalances → acercarse a GTO)

#### Scenario: vs Unknown → default (70%)
- **GIVEN** equity en boundary, villainType=Unknown
- **WHEN** se evalúa randomización
- **THEN** bet frequency = 70% (valor por defecto como antes)
