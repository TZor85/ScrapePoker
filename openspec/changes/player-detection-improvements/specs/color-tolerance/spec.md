# Color Detection Tolerance

## MODIFIED Requirements

### Requirement: Color detection SHALL use tolerance instead of exact match

The detection of playing and empty states via pixel color channel B shall use a tolerance of ±5 instead of exact value match. This handles antialiasing, transparency blending, and rendering variations.

#### Scenario: Exact match still works
- **GIVEN** pixel B channel = 17
- **AND** expected playing values = [17]
- **AND** tolerance = 5
- **WHEN** IsColorMatch is called
- **THEN** result SHALL be `true`

#### Scenario: Within tolerance — detected
- **GIVEN** pixel B channel = 18 (antialiased)
- **AND** expected playing values = [17]
- **AND** tolerance = 5
- **WHEN** IsColorMatch is called
- **THEN** result SHALL be `true`
- **AND** because |18 - 17| = 1 <= 5

#### Scenario: At tolerance boundary — detected
- **GIVEN** pixel B channel = 22
- **AND** expected playing values = [17]
- **AND** tolerance = 5
- **WHEN** IsColorMatch is called
- **THEN** result SHALL be `true`
- **AND** because |22 - 17| = 5 <= 5

#### Scenario: Outside tolerance — not detected
- **GIVEN** pixel B channel = 25
- **AND** expected playing values = [17]
- **AND** tolerance = 5
- **WHEN** IsColorMatch is called
- **THEN** result SHALL be `false`
- **AND** because |25 - 17| = 8 > 5

#### Scenario: Multiple expected values — any match within tolerance
- **GIVEN** pixel B channel = 16
- **AND** expected empty values = [14, 15, 53, 59, 74]
- **AND** tolerance = 5
- **WHEN** IsColorMatch is called
- **THEN** result SHALL be `true`
- **AND** because |16 - 15| = 1 <= 5

#### Scenario: Empty detection with tolerance
- **GIVEN** pixel B channel = 56 (near 53)
- **AND** expected empty values = [14, 15, 53, 59, 74]
- **AND** tolerance = 5
- **WHEN** IsColorMatch is called
- **THEN** result SHALL be `true`
- **AND** because |56 - 53| = 3 <= 5
