## ADDED Requirements

### Requirement: Validación de clave de licencia
El sistema SHALL validar una clave de licencia contra la base de datos PostgreSQL cada vez que la aplicación arranca. La validación SHALL verificar que la clave existe, está activa, no ha expirado y corresponde al dispositivo que la solicita.

#### Scenario: Clave válida primera activación
- **WHEN** el usuario introduce una clave válida que no tiene `HardwareId` asignado
- **THEN** el sistema asigna el `HardwareId` del dispositivo actual a la licencia, actualiza `LastValidation` y devuelve éxito con el rol de la licencia

#### Scenario: Clave válida dispositivo vinculado
- **WHEN** el usuario introduce una clave válida cuyo `HardwareId` coincide con el dispositivo actual
- **THEN** el sistema actualiza `LastValidation` y devuelve éxito con el rol de la licencia

#### Scenario: Clave inexistente
- **WHEN** el usuario introduce una clave que no existe en la BD
- **THEN** el sistema devuelve error con mensaje "Clave de licencia inválida"

#### Scenario: Licencia desactivada
- **WHEN** el usuario introduce una clave cuya propiedad `IsActive` es `false`
- **THEN** el sistema devuelve error con mensaje "Licencia desactivada"

#### Scenario: Licencia expirada
- **WHEN** el usuario introduce una clave de usuario (Role = User) cuya `ExpiresAt` es anterior a la fecha y hora actuales en UTC
- **THEN** el sistema devuelve error con mensaje "Licencia expirada el {fecha de expiración}"

#### Scenario: Licencia vinculada a otro dispositivo
- **WHEN** el usuario introduce una clave cuyo `HardwareId` no coincide con el del dispositivo actual
- **THEN** el sistema devuelve error con mensaje "Esta licencia ya está activada en otro dispositivo"

### Requirement: Bypass para licencias Admin
El sistema SHALL omitir la validación de hardware y expiración cuando el rol de la licencia sea `Admin`.

#### Scenario: Admin sin restricción de hardware
- **WHEN** el usuario introduce una clave con `Role = Admin` y `HardwareId` de la BD no coincide con el dispositivo actual (o es nulo)
- **THEN** el sistema NO vincula el hardware, actualiza `LastValidation` y devuelve éxito con `Role = Admin`

#### Scenario: Admin sin restricción de expiración
- **WHEN** el usuario introduce una clave con `Role = Admin` y `ExpiresAt` ha pasado
- **THEN** el sistema ignora la expiración y devuelve éxito con `Role = Admin`
