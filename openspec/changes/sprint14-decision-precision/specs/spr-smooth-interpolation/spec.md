## MODIFIED Requirements

### Requirement: Transición suave entre zonas de SPR
El ajuste de threshold por SPR SHALL usar interpolación lineal entre zonas en vez de buckets discretos, evitando saltos bruscos en las fronteras.

#### Scenario: SPR en zona push/fold (< 2.0) — interpolación desde 0
- **GIVEN** SPR = 1.0 (mitad de la zona push/fold 0-2.0)
- **WHEN** se calcula `GetSPRAdjustment`
- **THEN** `foldAdjust = -SPRPushFoldFoldReduction × (1 - spr/SPRPushFoldThreshold)` = -8 × (1 - 1.0/2.0) = **-4.0** (en vez de -8.0 flat)

#### Scenario: SPR justo debajo del threshold (1.9)
- **GIVEN** SPR = 1.9 (casi en zona normal)
- **WHEN** se calcula `GetSPRAdjustment`
- **THEN** `foldAdjust = -8 × (1 - 1.9/2.0)` = **-0.4** (casi sin ajuste, transición suave)

#### Scenario: SPR = 0 (all-in efectivo)
- **GIVEN** SPR = 0 (hero o villain all-in)
- **WHEN** se calcula `GetSPRAdjustment`
- **THEN** `foldAdjust = -SPRPushFoldFoldReduction` = **-8.0** (máximo ajuste)

#### Scenario: SPR en zona normal (2.0-4.0) — sin ajuste
- **GIVEN** SPR = 3.0 (zona normal)
- **WHEN** se calcula `GetSPRAdjustment`
- **THEN** `foldAdjust = 0, valueAdjust = 0, isPushFold = false` (sin cambios)

#### Scenario: SPR en zona deep (> 4.0) — interpolación hacia arriba
- **GIVEN** SPR = 6.0 (en zona deep, 2 unidades por encima del threshold)
- **WHEN** se calcula `GetSPRAdjustment`
- **THEN** `foldAdjust = SPRDeepFoldIncrease × min((spr - SPRDeepCautionThreshold) / 2.0, 1.0)` = 3.0 × min((6-4)/2, 1) = **3.0** (máximo alcanzado)

#### Scenario: SPR justo encima del threshold (4.1)
- **GIVEN** SPR = 4.1 (apenas en zona deep)
- **WHEN** se calcula `GetSPRAdjustment`
- **THEN** `foldAdjust = 3.0 × (0.1/2.0)` = **0.15** (casi sin ajuste, transición suave en vez de salto a 3.0)

#### Scenario: AdjustBetSizeForSPR también suavizado
- **GIVEN** SPR = 1.95 (justo debajo del threshold)
- **WHEN** se calcula bet sizing
- **THEN** se aplica un solo `IncreaseBetSize` (no doble), mismo que SPR = 1.5. La doble escalada solo se aplica con SPR <= 1.0.
