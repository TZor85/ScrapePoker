## 1. Dominio

- [ ] 1.1 Crear `src/OpenScrape.Domain/Enums/LicenseRole.cs` con valores `User` y `Admin`
- [ ] 1.2 Crear `src/OpenScrape.Domain/Entities/License.cs` con propiedades: `Id`, `LicenseKey`, `HardwareId`, `Role`, `CreatedAt`, `ExpiresAt`, `IsActive`, `LastValidation`, `OwnerName`

## 2. Hardware ID

- [ ] 2.1 Crear `src/OpenScrape.App/Helpers/HardwareIdHelper.cs` con método estático `GetHardwareId()` que combina `MachineName` + serial de disco (WMI) + MAC address y devuelve hash SHA256
- [ ] 2.2 Asegurar que la obtención de cada componente captura excepciones y usa cadena vacía como fallback

## 3. Servicio de licencia activa

- [ ] 3.1 Crear interfaz `src/OpenScrape.App/Services/ICurrentLicenseService.cs` con propiedades `LicenseKey`, `Role`, `IsAdmin` y método `Set(License)`
- [ ] 3.2 Crear implementación `src/OpenScrape.App/Services/CurrentLicenseService.cs` con estado inicial vacío (`LicenseKey = ""`, `Role = User`, `IsAdmin = false`)

## 4. Casos de uso de licencia

- [ ] 4.1 Crear `src/OpenScrape.Features/License/ValidateLicense.cs` con método `ExecuteAsync(string licenseKey, string hardwareId)` que implementa la lógica completa: buscar por clave, verificar `IsActive`, bypass admin (sin validar hardware ni expiración), validar expiración, vincular/comprobar hardware, actualizar `LastValidation`
- [ ] 4.2 Crear `src/OpenScrape.Features/License/LicenseUseCases.cs` que envuelve `ValidateLicense` y expone el caso de uso al formulario
- [ ] 4.3 Crear `src/OpenScrape.Features/License/AdminLicenseSeed.cs` con método `SeedAsync(IDocumentStore, string adminKey)` que verifica si existe licencia admin y la crea si no existe

## 5. Registro en DI e infraestructura

- [ ] 5.1 Modificar `src/OpenScrape.Features/Services.cs` para registrar `ValidateLicense`, `LicenseUseCases` y `AdminLicenseSeed` como `AddScoped`
- [ ] 5.2 Modificar `src/OpenScrape.Infrastructure/Services.cs` para añadir `options.Schema.For<License>().Index(x => x.LicenseKey)` en la configuración de Marten
- [ ] 5.3 Añadir sección `"AdminLicense": { "Key": "ADMIN-0000-0000-0001" }` en `src/OpenScrape.App/appsettings.json`

## 6. Formulario de login

- [ ] 6.1 Crear `src/OpenScrape.App/Forms/FrmLogin.Designer.cs` con el layout: label título, campo de texto para clave, checkbox "Recordar licencia", botón "Activar" y label de error
- [ ] 6.2 Crear `src/OpenScrape.App/Forms/FrmLogin.cs` con constructor que inyecta `LicenseUseCases` e `ICurrentLicenseService`, lógica de autocompletado desde `Properties/Settings`, manejo del botón "Activar" (deshabilitar durante validación, mostrar error o cerrar con `DialogResult.OK`), y guardado de clave si "Recordar licencia" está marcado
- [ ] 6.3 Registrar `FrmLogin` como `AddTransient` en `src/OpenScrape.App/Program.cs`

## 7. Flujo de arranque

- [ ] 7.1 Modificar `src/OpenScrape.App/Program.cs` para registrar `ICurrentLicenseService` como singleton (`CurrentLicenseService`)
- [ ] 7.2 Modificar `src/OpenScrape.App/Program.cs` para ejecutar el seed de licencia admin tras construir el Host (leer clave de configuración, llamar a `AdminLicenseSeed.SeedAsync`, capturar excepciones y loguear warning si falla)
- [ ] 7.3 Modificar `src/OpenScrape.App/Program.cs` para resolver `FrmLogin` desde el scope DI, llamar a `ShowDialog()` y solo continuar con `Application.Run(frmMain)` si el resultado es `DialogResult.OK`

## 8. Verificación

- [ ] 8.1 Compilar la solución completa con `dotnet build OpenScrape.sln` y resolver cualquier error
- [ ] 8.2 Ejecutar la app y verificar que aparece `FrmLogin` antes de `FrmMain`
- [ ] 8.3 Probar con la clave admin de `appsettings.json` y verificar que entra sin restricción de hardware
- [ ] 8.4 Probar con una clave inexistente y verificar que muestra el mensaje de error correcto
