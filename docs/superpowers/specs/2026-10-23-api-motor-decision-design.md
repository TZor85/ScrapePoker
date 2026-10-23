# API del motor de decisión — Diseño

## Propósito

Separar el motor de decisión de poker del cliente WinForms y exponerlo como un
servicio HTTP. El objetivo del usuario es **proteger y licenciar el motor**: el
binario que se distribuye al cliente no debe contener la lógica de equity, la
matriz postflop ni el sizing.

Estado de partida: `develop = 9d40445`.

## Alcance

### Dentro

- Extraer el motor (`UnifiedPokerCalculator`, `PokerDecisionFacade` y los
  servicios de `OpenScrape.DecisionMaker`) a proyectos sin dependencia de
  Windows.
- Contrato HTTP versionado para evaluar una decisión.
- Activación y validación de licencias por key, con cuota por plan.
- Cliente HTTP en la aplicación, con pantalla de activación.
- Pruebas de paridad entre el motor local actual y el servicio.

### Fuera

- OCR, captura de pantalla y overlay: dependen de la pantalla del usuario y de
  Tesseract/OpenCvSharp locales. No se mueven.
- Persistencia de manos, historial de oponentes y estadísticas de villanos en
  el servidor.
- Facturación, pasarela de pago y panel de administración.
- Binding de licencia a dispositivo.
- Multi-tenant, cuentas de usuario y recuperación de contraseña.

## Decisiones

| Decisión | Elegida | Alternativa descartada | Motivo |
|---|---|---|---|
| Alcance | Sólo el motor de decisión | Motor + persistencia / todo el backend | OCR y captura son locales; moverlos agrega latencia y expone la pantalla del usuario |
| Sin API disponible | El bot se detiene y avisa | Fallback degradado / motor local con licencia remota | Cualquier copia local del motor es extraíble y anula la protección |
| Identidad | License key por instalación | Cuenta de usuario / binding a device | Es lo más simple que igual protege; sin registro ni recuperación de password |
| Alojamiento | Contenedor agnóstico | Servicio managed / sólo local | Portable entre VPS, Azure y desarrollo; el proveedor se decide al desplegar |
| Historial de villanos | El cliente manda `VillainProfile` | El servidor acumula stats por alias | Evita datos personales en la infraestructura y mantiene el servicio stateless |
| UI de activación | `FormLicense` mínima | Sólo archivo de configuración | Sin pantalla, el usuario final no puede activar el producto |

## Arquitectura

```
OpenScrape.Domain  (entidades, enums, value objects — sin lógica de motor)
        ▲
        ├── OpenScrape.Contracts      DTOs del protocolo
        │         ▲
        │         ├── OpenScrape.DecisionMaker   motor puro
        │         │         ▲
        │         │         └── OpenScrape.Api   HTTP + licencias (Marten)
        │         │
        │         └── OpenScrape.App             cliente delgado (WinForms)
        │
        └── OpenScrape.Features / OpenScrape.Infrastructure  (sin cambios)
```

### `OpenScrape.Contracts` (nuevo)

`net10.0`. Referencia sólo `OpenScrape.Domain`. Sin Marten, sin ASP.NET, sin
paquetes de UI.

Contiene:

- `DecisionRequest` (movido desde `OpenScrape.DecisionMaker/DTOs`).
- `DecisionResult` (movido; se elimina `CalculationDetail`).
- `BetSizeCategory` (movido desde `OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`).
- `LicenseActivateRequest`, `LicenseActivateResponse`, `ApiError`.

`DecisionResult.CalculationDetail` se elimina: es `object?` y en JSON se
serializa como `{}`. Un consumidor que necesite el detalle debe recibir campos
explícitos, no un objeto opaco.

### `OpenScrape.DecisionMaker` (motor)

Ya es `net10.0`. Pierde la referencia a Marten (deuda del Sprint 1) y pasa a
referenciar `Contracts`.

Recibe desde `OpenScrape.App`:

- `Aplication/UseCases/UnifiedPokerCalculator.cs`
- `Services/PokerDecisionFacade.cs`
- `Services/IPokerDecisionFacade.cs`
- `Telemetry/IMetricsCollector.cs` y su implementación

`IDecisionTraceStore` queda como interfaz en el motor. La implementación con
Marten vive en `OpenScrape.Api`.

El motor no conoce HTTP, ni licencias, ni el cliente.

### `OpenScrape.Api` (nuevo)

`Microsoft.NET.Sdk.Web`, `net10.0`. Referencia `DecisionMaker`, `Contracts` y
Marten. Es el único proyecto que conoce ASP.NET.

Contiene: endpoints, middleware de licencia, rate limiting, `ILicenseStore` con
su implementación `MartenLicenseStore`, `IDecisionTraceStore` con
`MartenDecisionTraceStore`, configuración y Dockerfile. Las interfaces permiten
testear la API sin Postgres.

### `OpenScrape.App` (cliente delgado)

Deja de referenciar `OpenScrape.DecisionMaker`. Agrega:

- `Services/Http/HttpDecisionClient.cs` — implementa `IPokerDecisionFacade`.
- `Forms/FormLicense.cs` — activación.
- `Services/LicenseTokenStore.cs` — persiste key y token cifrados con `Encrypter`.

Conserva OCR, captura, overlay, `Features`, `Infrastructure` y `Domain`.

### Qué se mueve y qué se borra del cliente

| Archivo | Origen | Destino |
|---|---|---|
| `UnifiedPokerCalculator.cs` | `App/Aplication/UseCases` | `DecisionMaker/Aplication` |
| `PokerDecisionFacade.cs` | `App/Services` | `DecisionMaker/Services` |
| `IPokerDecisionFacade.cs` | `App/Services` | `DecisionMaker/Services` |
| `IMetricsCollector.cs` + impl | `App/Telemetry` | `DecisionMaker/Telemetry` |
| `DecisionRequest.cs`, `DecisionResult.cs` | `DecisionMaker/DTOs` | `Contracts` |
| `MartenDecisionTraceStore.cs` | `App/Services` | `Api/Stores` |

Los namespaces se ajustan al proyecto destino. No se cambia la lógica del motor
en esta migración.

## Contrato HTTP

Base: `/v1`. Versionado por ruta; un cambio incompatible crea `/v2`.

### `POST /v1/license/activate`

Anónimo, con rate limit por IP.

```json
{ "licenseKey": "OSPK-XXXX-XXXX-XXXX" }
```

`200`:

```json
{ "token": "<jwt>", "expiresAt": "2026-10-24T15:00:00Z", "plan": "pro" }
```

`401` key inexistente o revocada. `429` demasiados intentos.

### `POST /v1/decisions`

Requiere `Authorization: Bearer <jwt>`.

Cuerpo: `DecisionRequest`. Respuesta `200`: `DecisionResult`.

| Código | Situación |
|---|---|
| `400` | JSON inválido o campos requeridos ausentes |
| `401` | Token ausente, vencido o con firma inválida |
| `403` | Licencia revocada o plan sin acceso |
| `422` | Request válido pero imposible de evaluar (cartas duplicadas, calle inconsistente) |
| `429` | Cuota excedida |
| `500` | Error no controlado, sin detalles internos en el cuerpo |

Errores:

```json
{ "code": "license_revoked", "message": "La licencia fue revocada." }
```

### `GET /health`

Anónimo. `200` con `{ "status": "ok" }` si el proceso responde. No consulta la
base: es un liveness probe, no readiness.

## Licencias y seguridad

### Modelo

Documento `License` en Marten (esquema `license`):

| Campo | Tipo | Nota |
|---|---|---|
| `Id` | `Guid` | Identidad interna y claim `sub` |
| `KeyHash` | `string` | SHA-256 de `pepper + key`; la key en claro no se guarda |
| `Plan` | `string` | Define la cuota (`free`, `pro`) |
| `ExpiresAt` | `DateTimeOffset` | Vencimiento de la licencia |
| `Revoked` | `bool` | Revocación inmediata |
| `CreatedAt` | `DateTimeOffset` | Auditoría |

### Token

JWT HS256, firmado con `Jwt:SigningKey` (variable de entorno, mínimo 32 bytes).
Claims: `sub` (id de licencia), `plan`, `iat`, `exp`. Vida: 24 h. El cliente lo
guarda cifrado; al vencer, reactiva con la misma key sin intervención.

Revocar una licencia no invalida tokens ya emitidos hasta que vencen. Si se
necesita corte inmediato, el middleware consulta `Revoked` en cada request; se
deja como opción configurable (`License:CheckRevocationPerRequest`), apagada por
defecto.

### Cuotas

Rate limiting por `sub`, ventana fija de un minuto. `pro`: 60 req/min. `free`:
10 req/min. Configurable por `RateLimit:Plans:<plan>:PermitsPerMinute`. Al
exceder: `429`.

### CLI de licencias

`src/Tools/LicenseCli`. Comandos:

```
license create --plan pro --days 365   → imprime la key en claro una sola vez
license revoke --id <guid>
license list
```

Se conecta a la misma base por `ConnectionStrings__Marten`. No hay panel web.

## Cliente

### `HttpDecisionClient`

Implementa `IPokerDecisionFacade`. `HttpClient` tipado con
`BaseAddress = Api:BaseUrl` y `Timeout = 1,5 s`. Sin reintentos: reintentar
dentro del turno duplica la latencia.

- Adjunta el Bearer desde `LicenseTokenStore`.
- `401`/`403`: borra el token y devuelve un error de licencia que la UI muestra
  pidiendo reactivación.
- Timeout, `5xx` o error de red: lanza `EngineUnavailableException`.
- No conoce DTOs internos del motor; sólo `Contracts`.

### Sin fallback

Cuando `FrmMain` recibe `EngineUnavailableException`:

1. Detiene el loop de detección.
2. Muestra el error en el log box con la causa.
3. No emite decisión ni acción.

El bot se detiene. Es el comportamiento elegido: sin motor local no hay nada que
extraer del binario.

### `FormLicense`

Visible desde el menú de `FrmMain` y al arrancar si no hay token válido. Un
textbox para la key, un botón **Validar**, el plan y el vencimiento. Al validar
llama a `/v1/license/activate` y persiste key y token cifrados.

La key nunca se escribe en logs.

## Datos y privacidad

- El request **no** incluye nombres de jugadores. `VillainId` se envía como
  `"Unknown"` o como un identificador opaco generado por el cliente.
- El perfil del villano viaja en `VillainProfile`; el servidor no acumula
  historial.
- `MartenDecisionTraceStore` persiste en el servidor la traza de cada decisión
  (request sin datos personales + resultado). Sirve de auditoría de uso.
- El servidor no guarda imágenes ni texto de OCR.

## Migración por fases

Tres fases, cada una mergeable y reversible. Flag `RemoteEngine:Enabled` en el
cliente, apagado por defecto hasta la fase 3.

### Fase 1 — Punto de entrada único

`FrmMain` deja de llamar a `IPokerCalculator` y `IPostflopDecisionService` por
separado y pasa a consumir `IPokerDecisionFacade` con la implementación local.
La facade ya existe y ya absorbe esas dependencias; hoy sólo la usan los tests.

Salida: paridad verificada con fixtures. Sin cambios de comportamiento
observable.

### Fase 2 — Servicio y cliente HTTP

Se crean `Contracts`, `OpenScrape.Api` y `HttpDecisionClient`. Con
`RemoteEngine:Enabled = false` el cliente sigue usando el motor local; con el
flag encendido usa la API. Ambos caminos conviven para comparar.

Salida: `/v1/decisions` responde con paridad y el cliente funciona en modo
remoto.

### Fase 3 — Retirar el motor del cliente

Se borran del cliente el motor y la facade local, se elimina el flag y queda
sólo `HttpDecisionClient`. Se verifica el publish.

Salida: el artefacto publicado no contiene `OpenScrape.DecisionMaker.dll`.

## Pruebas

### Unitarias del motor

Las existentes se mantienen. `OpenScrape.App.Tests` ya referencia
`OpenScrape.DecisionMaker` de forma directa, así que los tests del motor siguen
compilando cuando `App` deje de referenciarlo. El test de arquitectura valida el
csproj de `App` y el publish, no el proyecto de tests.

### Contrato

Serialización y deserialización de cada DTO de `Contracts`, incluidos enums y
campos opcionales. Un cambio de forma rompe el test antes de romper al cliente.

### Integración de la API

`WebApplicationFactory` con `ILicenseStore` en memoria. Casos: activación
válida, key inexistente, token vencido, token con firma inválida, licencia
revocada, cuota excedida, request inválido, decisión imposible.

Marten se usa sólo en producción; los tests no requieren Postgres.

### Paridad (fixtures dorados)

Para cada captura en `output/samples/grab` se genera un `DecisionRequest` y se
guarda el `DecisionResult` del motor local en `test/fixtures/decisions/`.

La suite replaya cada fixture contra la API y compara:

- `RecommendedAction` exacta.
- `EquityPercent` con tolerancia ±0,1.
- `BetSize` con tolerancia ±0,1.
- `Reason` no vacío.

Un fixture que no coincide bloquea el merge.

### Arquitectura

- `OpenScrape.App.csproj` no contiene `ProjectReference` a `DecisionMaker`.
- El directorio de publish del cliente no contiene `OpenScrape.DecisionMaker.dll`
  ni `OpenScrape.Contracts` con tipos del motor.
- `OpenScrape.Contracts` no referencia Marten ni ASP.NET.

## Despliegue

- Dockerfile multi-stage en `src/OpenScrape.Api/Dockerfile`; contexto de build
  en la raíz del repo.
- `GET /health` como healthcheck del contenedor.
- Configuración por entorno, nunca en el repo:
  - `Jwt__SigningKey` (32+ bytes)
  - `Jwt__Issuer`
  - `ConnectionStrings__Marten`
  - `License__Pepper`
  - `RateLimit__Plans__pro__PermitsPerMinute`
  - `ASPNETCORE_URLS`
- HTTPS obligatorio fuera de desarrollo.
- Logs estructurados con id de licencia, nunca con la key ni con el token.

## Riesgos

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Latencia de red en el turno | El bot decide tarde | Timeout 1,5 s, servidor en la misma región que el usuario, medir p95 |
| Caída del servicio | El bot se detiene | Monitoreo y reinicio automático; aceptado por diseño |
| Key compartida entre instalaciones | Pérdida de ingresos | Cuota por token y rate limit; device binding queda como fase futura |
| Deriva del contrato | Cliente y API incompatibles | `Contracts` compartido + tests de contrato + versión en la ruta |
| Regresión al mover el motor | Decisiones distintas | Fixtures dorados y fase 1 sin cambio de comportamiento |
| Secreto JWT filtrado | Tokens falsificables | Variable de entorno, rotación, gitleaks ya activo en CI |

## Criterios de aceptación

1. `OpenScrape.Api` responde `/v1/decisions` con paridad 4/4 sobre los fixtures.
2. El publish del cliente no incluye el motor.
3. Suite completa en verde y 0 advertencias en Release.
4. `dotnet format --verify-no-changes` sin cambios.
5. `verify-pre-merge.ps1 -SkipSmoke` con todos los checkpoints en PASS.
