## ADDED Requirements

### Requirement: Almacenar licencia activa en sesión
El sistema SHALL mantener un singleton `ICurrentLicenseService` que almacene los datos de la licencia validada durante la sesión actual. Otros componentes SHALL poder inyectarlo para consultar el rol.

#### Scenario: Poblar servicio tras login exitoso
- **WHEN** la validación de licencia devuelve éxito
- **THEN** `ICurrentLicenseService` queda poblado con `LicenseKey`, `Role` y `IsAdmin` correspondientes

#### Scenario: Consultar si es admin
- **WHEN** cualquier componente inyecta `ICurrentLicenseService` y consulta `IsAdmin`
- **THEN** devuelve `true` si el rol es `Admin`, `false` en caso contrario

#### Scenario: Estado inicial sin login
- **WHEN** se accede a `ICurrentLicenseService` antes de completar el login
- **THEN** `LicenseKey` es cadena vacía, `Role` es `User` e `IsAdmin` es `false`
