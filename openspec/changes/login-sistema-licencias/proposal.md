## Why

La aplicación OpenScrape no tiene ningún mecanismo de acceso controlado: cualquier persona que obtenga el binario puede usarla sin restricciones. Se necesita un sistema de licencias que permita distribuir la app comercialmente, controlar quién puede usarla, y contar con un acceso de administrador para pruebas en producción.

## What Changes

- Se añade un formulario de login (`FrmLogin`) que aparece antes de `FrmMain` al arrancar la app.
- Se introduce la entidad `License` en el dominio, persistida en PostgreSQL vía Marten.
- Se implementa la validación de licencia online contra la base de datos en cada arranque.
- Las licencias son temporales (con fecha de expiración) y se vinculan a un único dispositivo (hardware ID).
- Existe un rol `Admin` cuya licencia nunca expira y no tiene restricción de hardware (bypass para pruebas en producción).
- La licencia activa se expone mediante `ICurrentLicenseService` para que otros módulos puedan consultar el rol.
- Se añade un seed automático de la licencia admin al arrancar si no existe en la BD.

## Capabilities

### New Capabilities

- `license-validation`: Validación de clave de licencia contra PostgreSQL, con lógica de vinculación de hardware, comprobación de expiración y bypass para rol Admin.
- `login-form`: Formulario WinForms modal previo a `FrmMain` que solicita la clave de licencia, muestra errores y permite recordar la clave entre sesiones.
- `hardware-id`: Generación determinista de un identificador único de la máquina a partir de hardware disponible en Windows.
- `current-license-service`: Servicio singleton que almacena los datos de la licencia activa durante la sesión y expone el rol del usuario.
- `admin-license-seed`: Creación automática de la licencia admin en la BD al primer arranque si no existe, usando la clave definida en configuración.

### Modified Capabilities

## Impact

- **`src/OpenScrape.App/Program.cs`**: Cambia el flujo de arranque para mostrar `FrmLogin` antes de `FrmMain`.
- **`src/OpenScrape.Domain/`**: Se añaden `Entities/License.cs` y `Enums/LicenseRole.cs`.
- **`src/OpenScrape.Features/`**: Se añade la carpeta `License/` con casos de uso. Se modifica `Services.cs` para registrarlos.
- **`src/OpenScrape.Infrastructure/Services.cs`**: Se configura el schema de Marten para `License` con índice en `LicenseKey`.
- **`src/OpenScrape.App/Forms/`**: Se añade `FrmLogin.cs` y `FrmLogin.Designer.cs`.
- **`src/OpenScrape.App/Helpers/`**: Se añade `HardwareIdHelper.cs`.
- **`src/OpenScrape.App/Services/`**: Se añade `CurrentLicenseService.cs` e `ICurrentLicenseService.cs`.
- **`appsettings.json`**: Se añade la sección `AdminLicense` con la clave maestra de admin.
- **Dependencias**: No se añaden paquetes NuGet nuevos; se usa `System.Management` (ya disponible en .NET 10 Windows) para el hardware ID.
