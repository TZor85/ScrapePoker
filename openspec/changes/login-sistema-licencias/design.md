## Context

OpenScrape es una app WinForms (.NET 10.0) con arquitectura Clean Architecture en capas: `Domain`, `Features`, `Infrastructure` y `App`. Usa Marten (document store sobre PostgreSQL/Neon) como base de datos y Microsoft.Extensions.Hosting para DI.

El flujo de arranque actual en `Program.cs` construye el Host, resuelve `FrmMain` desde DI y ejecuta `Application.Run(form)`. No existe ningún mecanismo de autenticación ni control de acceso.

La app ya tiene una clave de encriptación en `appsettings.json` (`Encrypter.Key`) y usa Marten con `AutoCreate.All` en Development, lo que facilita la creación automática de tablas para nuevas entidades.

## Goals / Non-Goals

**Goals:**
- Bloquear el arranque de la app sin una licencia válida.
- Validar la licencia online contra PostgreSQL en cada arranque.
- Vincular cada licencia de usuario a un único dispositivo (hardware ID).
- Proveer una licencia de administrador sin restricción de hardware ni expiración para pruebas en producción.
- Exponer el rol de la licencia activa al resto de la app mediante un servicio inyectable.
- Seed automático de la licencia admin al primer arranque si no existe en la BD.

**Non-Goals:**
- Registro de nuevos usuarios desde la app (las licencias se crean manualmente o desde el panel admin).
- Uso offline / caché local de licencias.
- Control de funcionalidades por nivel de licencia (básico vs premium).
- Interfaz de gestión de licencias en esta iteración.

## Decisions

### 1. Entidad `License` en Marten como documento raíz

**Decisión**: `License` es un documento raíz de Marten con índice único sobre `LicenseKey`.

**Alternativas consideradas**:
- SQL puro con Npgsql: más control pero rompe el patrón Marten ya establecido.
- Archivo de configuración cifrado local: no permite control centralizado ni revocación.

**Rationale**: Marten ya está configurado y con `AutoCreate.All` la tabla se crea sola. El índice único en `LicenseKey` garantiza eficiencia en la búsqueda y unicidad.

---

### 2. Hardware ID basado en SHA256 de datos de la máquina

**Decisión**: `HardwareIdHelper` combina `Environment.MachineName` + número de serie del primer disco fijo (vía `System.Management` / WMI) + primera MAC address activa, y calcula un hash SHA256 de la concatenación.

**Alternativas consideradas**:
- Solo `MachineName`: demasiado fácil de falsificar.
- Solo MAC: cambia si se cambia la tarjeta de red o se usa VPN.
- Combinación de los tres: más robusto ante cambios parciales de hardware.

**Rationale**: La combinación de tres fuentes hace el ID estable en el uso normal y difícil de duplicar. Si algún componente no está disponible (VM sin disco físico, etc.) se usa una cadena vacía para ese componente sin lanzar excepción.

---

### 3. Bypass admin: licencia en BD con `Role = Admin`

**Decisión**: La licencia admin no está hardcodeada en código. Se persiste en la BD con `Role = Admin`, `ExpiresAt = DateTime.MaxValue` y sin restricción de `HardwareId`. La clave admin se define en `appsettings.json` bajo `AdminLicense:Key`.

**Alternativas consideradas**:
- Clave hardcodeada en código: fácil de extraer por decompilación.
- Variable de entorno `OPENSCRAPE_BYPASS=true`: no registra quién entra ni cuándo.

**Rationale**: Al estar en la BD, la licencia admin deja rastro de `LastValidation` y puede desactivarse remotamente si es necesario. La clave en `appsettings.json` es configurable por entorno sin recompilar.

---

### 4. Flujo de arranque: `FrmLogin.ShowDialog()` antes de `FrmMain`

**Decisión**: En `Program.cs`, después de construir el Host y ejecutar el seed, se resuelve `FrmLogin` desde DI y se llama a `ShowDialog()`. Solo si el resultado es `DialogResult.OK` se continúa con `Application.Run(frmMain)`.

**Alternativas consideradas**:
- Panel/overlay dentro de `FrmMain`: acopla la lógica de auth a la UI principal.
- Splash screen: más complejo, no aporta valor funcional adicional.

**Rationale**: El formulario modal separado mantiene la lógica de login completamente aislada de `FrmMain` y permite cerrar la app limpiamente si el usuario cancela o la licencia es inválida.

---

### 5. `ICurrentLicenseService` como singleton en DI

**Decisión**: Tras una validación exitosa, `FrmLogin` puebla un singleton `CurrentLicenseService` que implementa `ICurrentLicenseService` y expone `LicenseKey`, `Role` e `IsAdmin`. `FrmMain` y otros servicios pueden inyectarlo.

**Alternativas consideradas**:
- Propiedad estática global: difícil de testear y viola los principios DI.
- Pasar el rol como parámetro al constructor de `FrmMain`: funciona pero no escala a más componentes.

**Rationale**: El patrón singleton en DI es el estándar para estado de sesión en aplicaciones de escritorio con Microsoft.Extensions.DependencyInjection.

---

### 6. Seed de licencia admin al arranque

**Decisión**: En `Program.cs`, tras construir el Host y antes de mostrar `FrmLogin`, se ejecuta un método `SeedAdminLicenseAsync()` que abre una sesión Marten, busca si existe alguna licencia con `Role = Admin` y si no existe, la crea usando la clave de `appsettings.json`.

**Rationale**: Garantiza que siempre haya al menos una licencia admin disponible. El seed es idempotente: si ya existe, no hace nada. No interrumpe el flujo normal si falla (solo logea un warning).

## Risks / Trade-offs

- **[Riesgo] Cambio de hardware invalida la licencia de usuario** → El usuario deberá contactar al admin para reactivar (desvinculación manual en BD). No hay flujo automático de recuperación en esta versión.
- **[Riesgo] `System.Management` (WMI) puede no estar disponible en algunas VMs** → Se captura la excepción y se usa cadena vacía para ese componente; el hardware ID resultante será menos único pero seguirá funcionando.
- **[Riesgo] Clave admin en `appsettings.json` en texto plano** → Para mayor seguridad debería moverse a un user secret o variable de entorno. En esta iteración se acepta el trade-off por simplicidad; el riesgo es bajo porque el fichero no se distribuye con el binario.
- **[Trade-off] Validación siempre online** → Requiere conexión a internet en cada arranque. La BD Neon tiene alta disponibilidad, pero si cae, ningún usuario (ni admin) puede entrar. Se acepta por simplicidad; una caché offline se puede añadir en el futuro.

## Migration Plan

1. Aplicar los cambios de código.
2. Al arrancar la app por primera vez, Marten crea automáticamente la tabla de `License` y el índice (`AutoCreate.All` en Development / ejecutar `dotnet run` o migration en producción).
3. Verificar que la licencia admin se ha creado correctamente revisando los logs de arranque.
4. Crear licencias de prueba para usuarios usando el método de utilidad o directamente en la BD.

**Rollback**: Revertir los cambios de código. La tabla `License` en la BD puede dejarse sin datos o eliminarse manualmente si es necesario.

## Open Questions

- ¿Cómo se entregan las claves de licencia a los usuarios finales? (email, panel web, etc.) — fuera del alcance de este change.
- ¿Se necesita un panel de administración en la app para crear/revocar licencias? — pospuesto para una iteración futura.
