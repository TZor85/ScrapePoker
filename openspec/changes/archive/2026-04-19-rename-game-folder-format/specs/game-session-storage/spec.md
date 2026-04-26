## MODIFIED Requirements

### Requirement: Formato de nombre de carpeta de sesión
Las carpetas de sesión en `resources/Games` deberán usar el formato `AAAAMMDD_Game` (ej. `20260419_Game`).

#### Scenario: Carpeta con fecha correcta
- **WHEN** se guarda una nueva sesión el 19 de abril de 2026
- **THEN** el sistema crea la carpeta `resources/Games/20260419_Game/`