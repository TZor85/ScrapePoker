## MODIFIED Requirements

### Requirement: Check-raise bloqueado cuando SPR compromete el stack
Antes de ejecutar un check-raise, el sistema SHALL verificar si el SPR es tan bajo que el raise compromete al hero a ir all-in, y exigir equity mínima en ese caso.

#### Scenario: Check-raise con SPR >= 2.0 — sin restricción extra
- **GIVEN** hero OOP con TwoPair, SPR = 3.0, equity = 55%
- **WHEN** se evalúa check-raise y `equity > thresholds.CheckRaiseThreshold`
- **THEN** se ejecuta check-raise normalmente (SPR suficiente para maniobrar)

#### Scenario: Check-raise con SPR < 1.5 y equity >= 60% — permitido
- **GIVEN** hero OOP con TwoPair, SPR = 1.2, equity = 65%
- **WHEN** se evalúa check-raise
- **THEN** se permite el check-raise (equity justifica el commit all-in implícito)

#### Scenario: Check-raise con SPR < 1.5 y equity < 60% — bloqueado
- **GIVEN** hero OOP con TwoPair en board wet, SPR = 1.0, equity = 50%
- **WHEN** se evalúa check-raise
- **THEN** NO se ejecuta check-raise (equity insuficiente para commit all-in). Se procede al siguiente path de decisión (bet normal o check).

#### Scenario: Check-raise draw con SPR < 1.5 — siempre bloqueado
- **GIVEN** hero OOP con flush draw (9 outs), SPR = 1.3, equity = 42%
- **WHEN** se evalúa check-raise semi-bluff
- **THEN** NO se ejecuta check-raise (con SPR tan bajo, el semi-bluff pierde fold equity y se convierte en all-in puro)

#### Scenario: IP trap con SPR < 1.5 — bloqueado
- **GIVEN** hero IP con TwoPair, SPR = 1.1, equity = 55%
- **WHEN** se evalúa IP trap check-raise
- **THEN** NO se ejecuta check-raise. Mejor apostar directamente para controlar el sizing.

### Requirement: Nuevo parámetro CheckRaiseSPRMinThreshold
SHALL existir un parámetro `CheckRaiseSPRMinThreshold` (default 1.5) en StrategyProfile que define el SPR mínimo para permitir check-raise sin equity premium.

### Requirement: Nuevo parámetro CheckRaiseLowSPRMinEquity
SHALL existir un parámetro `CheckRaiseLowSPRMinEquity` (default 60.0) en StrategyProfile que define la equity mínima para check-raise cuando SPR < CheckRaiseSPRMinThreshold.
