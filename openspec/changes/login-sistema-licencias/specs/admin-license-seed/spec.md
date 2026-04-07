## ADDED Requirements

### Requirement: Creación automática de licencia admin al primer arranque
El sistema SHALL verificar al arrancar si existe alguna licencia con `Role = Admin` en la BD. Si no existe, SHALL crear una usando la clave definida en `appsettings.json` bajo `AdminLicense:Key`, con `ExpiresAt = DateTime.MaxValue` e `IsActive = true`. La operación SHALL ser idempotente.

#### Scenario: No existe licencia admin
- **WHEN** la aplicación arranca y no hay ninguna licencia con `Role = Admin` en la BD
- **THEN** se crea automáticamente una licencia admin con la clave de `appsettings.json`, `Role = Admin`, `ExpiresAt = DateTime.MaxValue` e `IsActive = true`

#### Scenario: Ya existe licencia admin
- **WHEN** la aplicación arranca y ya existe al menos una licencia con `Role = Admin` en la BD
- **THEN** no se crea ninguna licencia nueva y el arranque continúa normalmente

#### Scenario: Fallo en el seed no bloquea la app
- **WHEN** el seed de licencia admin falla por un error de BD
- **THEN** el error se registra en los logs con nivel Warning y la aplicación continúa mostrando `FrmLogin`
