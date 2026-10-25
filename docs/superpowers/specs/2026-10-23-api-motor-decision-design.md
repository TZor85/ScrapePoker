# API del motor de decisión — Diseño

## Propósito

Separar el motor de decisión de poker del cliente WinForms y exponerlo como un
servicio HTTP. El objetivo del usuario es **proteger y licenciar el motor**: el
binario que se distribuye al cliente no debe contener la lógica de equity, la
matriz postflop, el sizing ni la tabla de rangos preflop.

Estado de partida: `develop = 9d40445`.

## Alcance

### Dentro

- Extraer **las dos ramas de decisión** a proyectos sin dependencia de Windows:
  - **Preflop**: `SetPreflopActionUseCase`, los diez use cases de acción,
    `UserHandHelper` y `GetActionScenario` con su tabla de rangos.
  - **Postflop**: la lógica de decisión de `GameCoordinator`, más
    `UnifiedPokerCalculator`, `PostflopDecisionService` y el sizing.
- Contrato HTTP versionado para evaluar una decisión, con una sola puerta de
  entrada que cubre las cuatro calles.
- Ejecutar en el servidor todo lo que es función de las cartas: equity, outs,
  pot odds, textura y ajuste de c-bet. El cliente manda hechos, no resultados.
- Activación y validación de licencias por key, con cuota por plan.
- Cliente HTTP en la aplicación, con pantalla de activación.
- Pruebas de paridad entre el motor local actual y el servicio.

### Fuera

- OCR, captura de pantalla y overlay: dependen de la pantalla del usuario y de
  Tesseract/OpenCvSharp locales. No se mueven.
- El estado cross-street, el tracking de villanos, el cálculo de exploitability
  y el logging de decisiones: se quedan en el cliente. El servidor es stateless
  y recibe todo lo que necesita en el request.
- Persistencia de manos, historial de oponentes y estadísticas de villanos en
  el servidor.
- Facturación, pasarela de pago y panel de administración.
- Binding de licencia a dispositivo.
- Multi-tenant, cuentas de usuario y recuperación de contraseña.

## Decisiones

| Decisión | Elegida | Alternativa descartada | Motivo |
|---|---|---|---|
| Alcance | Preflop y postflop desde el arranque | Sólo postflop, preflop en una fase posterior | La tabla de rangos y la inferencia de situación son el núcleo vendible; dejarlas en el cliente anula la protección |
| Sin API disponible | El bot se detiene y avisa | Fallback degradado / motor local con licencia remota | Cualquier copia local del motor es extraíble y anula la protección |
| Identidad | License key por instalación | Cuenta de usuario / binding a device | Es lo más simple que igual protege; sin registro ni recuperación de password |
| Alojamiento | Contenedor agnóstico | Servicio managed / sólo local | Portable entre VPS, Azure y desarrollo; el proveedor se decide al desplegar |
| Historial de villanos | El cliente manda `VillainProfile` | El servidor acumula stats por alias | Evita datos personales en la infraestructura y mantiene el servicio stateless |
| UI de activación | `FormLicense` mínima | Sólo archivo de configuración | Sin pantalla, el usuario final no puede activar el producto |
| Cálculo previo a la decisión | El servidor calcula equity, outs, pot odds, textura y ajuste de c-bet | El cliente manda esos valores ya resueltos | Si el cliente los calcula, el motor sigue en el binario y no hay nada que licenciar |
| Acceso a la tabla de rangos | `IPreflopRangeStore` en el motor | `DecisionMaker` referencia `Features` | El motor no puede arrastrar Marten; la implementación vive en la API |

## Hallazgos que corrigen el diseño inicial

El diseño anterior suponía que `FrmMain` consumía `IPokerDecisionFacade`. **No es
así**, y la corrección cambia la fase 1:

- `FrmMain` llama directo a `IPokerCalculator.Calculate` en cinco lugares y
  decide con `GameCoordinator.DetermineFlopAction/Turn/River`.
- `GameCoordinator` arma el `PostflopDecisionInput` con datos que la facade no
  calcula: `EffectiveOuts`, `HeroKickerStrength`, `VillainFoldToBetPct`, la
  equity ajustada por `PreflopAnalyzer.CalculateCbetAdjustment`, el donk bet y
  el board change. La facade calcula equity y textura por su cuenta: **no es
  equivalente**.
- Nadie ejercita la facade en producción, así que la suite actual no detecta la
  divergencia.
- El preflop no pasa por `GameCoordinator` ni por la facade: va por
  `SetPreflopActionUseCase` → use cases de acción → `GetActionScenario`
  (`OpenScrape.Features`) → Marten.

Conclusión: no se "conecta la facade". Se **extrae la lógica de decisión real**
de `GameCoordinator` a un engine puro, se **mueve el motor preflop** al mismo
engine, y la paridad se prueba con fixtures generados desde el código actual
antes de tocarlo. La facade se elimina.

### Las tres ramas de decisión hoy

| Rama | Punto de entrada | Lógica | Datos |
|---|---|---|---|
| Preflop | `FrmMain.ProcessPreflopAsync` | `SetPreflopActionUseCase` + 10 use cases | `GetActionScenario` → Marten |
| Postflop | `GameCoordinator.Determine*Action` | `PostflopDecisionService` + sizing | `IPokerCalculator` |
| Muerta | — | `PokerDecisionFacade` | Nadie la usa |

## Arquitectura

```
OpenScrape.Domain  (entidades, enums, value objects, PlayerGameState)
        ▲
        ├── OpenScrape.Contracts           DTOs del protocolo
        │         ▲
        │         ├── OpenScrape.DecisionMaker   motor puro (preflop + postflop)
        │         │         ▲
        │         │         └── OpenScrape.Api   HTTP + licencias (Marten)
        │         │
        │         └── OpenScrape.App             cliente delgado (WinForms)
        │
        └── OpenScrape.Features / OpenScrape.Infrastructure
                        ▲
                        └── OpenScrape.Api implementa IPreflopRangeStore sobre GetTable
```

### `OpenScrape.Contracts` (nuevo)

`net10.0`. Referencia sólo `OpenScrape.Domain`. Sin Marten, sin ASP.NET, sin
paquetes de UI.

Contiene:

- `DecisionRequest`, con `Street` y los bloques `Preflop`/`Postflop`.
- `DecisionResult`.
- `BetSizeCategory` (hoy atrapado en `PostflopDecisionService`).
- `LicenseActivateRequest`, `LicenseActivateResponse`, `ApiError`.

`DecisionResult.CalculationDetail` se elimina: es `object?` y en JSON se
serializa como `{}`. Un consumidor que necesite el detalle debe recibir campos
explícitos, no un objeto opaco.

### `OpenScrape.DecisionMaker` (motor)

Ya es `net10.0`. Pierde la referencia a Marten y pasa a referenciar `Contracts`.

Recibe desde `OpenScrape.App`:

- `Aplication/UseCases/UnifiedPokerCalculator.cs`.
- `Aplication/SetPreflopActionUseCase.cs` y los diez `UseCases/Actions/*`.
- `Helpers/UserHandHelper.cs`.
- `Features/ActionScenario` (`GetActionScenario`, `ActionScenarioRequest`).
- La lógica de decisión de `Services/GameCoordinator.cs` extraída a
  `PostflopDecisionEngine`.
- `Telemetry/IMetricsCollector.cs` y su implementación.

Define:

- `IDecisionEngine` — única puerta: `Task<DecisionResult> DecideAsync(DecisionRequest, CancellationToken)`.
- `IPreflopRangeStore` — `Task<TableDTO?> GetAsync(string situation)`. La
  implementación con Marten vive en `OpenScrape.Api`.
- `PostflopDecisionEngine` — construye el `PostflopDecisionInput` que hoy arma
  `GameCoordinator`, sin efectos secundarios.
- `IRandomProvider` se mantiene inyectable: la mezcla ponderada preflop es
  estocástica y debe poder fijarse en los tests.

`IDecisionTraceStore` queda como interfaz en el motor. La implementación con
Marten vive en `OpenScrape.Api`.

El motor no conoce HTTP, ni licencias, ni el cliente.

### `OpenScrape.Api` (nuevo)

`Microsoft.NET.Sdk.Web`, `net10.0`. Referencia `DecisionMaker`, `Contracts`,
`Features` y Marten. Es el único proyecto que conoce ASP.NET.

Contiene: endpoints, middleware de licencia, rate limiting, `ILicenseStore` con
`MartenLicenseStore`, `IPreflopRangeStore` con `MartenPreflopRangeStore` (envuelve
`GetTable`), `IDecisionTraceStore` con `MartenDecisionTraceStore`, configuración
y Dockerfile. Las interfaces permiten testear la API sin Postgres.

### `OpenScrape.App` (cliente delgado)

En la fase 3a sigue referenciando `OpenScrape.DecisionMaker`, porque el camino
local convive con el remoto para comparar; lo pierde en la fase 4. Agrega:

- `Services/Http/HttpDecisionEngine.cs` — implementa `IDecisionEngine` contra la
  API.
- `Services/PlayerStateMapper.cs` — traduce `PlayerGameState`, el board y el
  contexto al `DecisionRequest` crudo.
- `Forms/FormLicense.cs` — activación (fase 3b).
- `Services/LicenseTokenStore.cs` — persiste key y token cifrados con
  `Encrypter` (fase 3b).

`GameCoordinator` se queda en `App` como adaptador: conserva el estado
cross-street, el tracking de villanos, la exploitability y el logging, y delega
la decisión en `IDecisionEngine`.

Ese recableado es parte de la fase 3a y no es cosmético: hoy el coordinador
llama directo a `PostflopDecisionEngine` y a los use cases preflop. Pasa a armar
un `DecisionRequest`, pedir la decisión a `IDecisionEngine` y recién después
hacer sus efectos (tracking, contexto, logging, exploitability) con el
resultado. Sin ese cambio, el flag `RemoteEngine:Enabled` no tendría efecto.

Conserva OCR, captura, overlay, `Features`, `Infrastructure` y `Domain`.

### Qué se mueve y qué se borra del cliente

| Archivo | Origen | Destino |
|---|---|---|
| `PlayerGameState.cs`, `Player.cs`, `BoardData`, `ResponseAction` | `App/Entities` | `Domain/Entities` |
| `UnifiedPokerCalculator.cs` | `App/Aplication/UseCases` | `DecisionMaker` |
| `SetPreflopActionUseCase.cs` + 10 use cases | `App/Aplication` | `DecisionMaker/Preflop` |
| `UserHandHelper.cs` | `App/Helpers` | `DecisionMaker/Preflop` |
| `GetActionScenario.cs`, `ActionScenarioRequest.cs` | `Features/ActionScenario` | `DecisionMaker/Preflop` |
| Lógica de decisión de `GameCoordinator` | `App/Services` | `DecisionMaker/Postflop` |
| `PokerDecisionFacade.cs` + `IPokerDecisionFacade.cs` | `App/Services` | **Se borran** |
| `DecisionRequest.cs`, `DecisionResult.cs` | `DecisionMaker/DTOs` | `Contracts` |
| `BetSizeCategory` | `PostflopDecisionService` | `Contracts` |
| `MartenDecisionTraceStore.cs` | `App/Services` | `Api/Stores` |

Los namespaces se ajustan al proyecto destino. **No se cambia la lógica del
motor en esta migración**: sólo se mueve y se le inyectan sus dependencias.

Hoy conviven dos `DecisionRequest` y dos `DecisionResult` homónimos: los de
`DecisionMaker/DTOs`, que describen la entrada interna del motor, y los de la
puerta `IDecisionEngine`. En `Contracts` queda una sola definición de cada uno,
con los campos crudos que viajan por HTTP. `IOpponentTracker` no se mueve: su
estado lo produce el cliente y viaja como `VillainProfile` en el request.

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

```json
{
  "street": "Preflop",
  "hero": { "card1": { "rank": 12, "suit": 2 }, "card2": { "rank": 8, "suit": 2 } },
  "position": "Button",
  "potSize": 2.5,
  "heroStack": 100,
  "isSecondAction": false,
  "seats": [
    { "position": "UTG", "bet": 1, "active": true },
    { "position": "Button", "bet": 0, "active": true }
  ],
  "villainProfile": { "handsPlayed": 40, "timesVoluntarilyPutMoneyIn": 12 }
}
```

Para `Flop`/`Turn`/`River` el request agrega `board` (tres, cuatro o cinco
cartas), `betToCall`, los stacks de los villanos y `context`: los hechos de la
calle que sólo el cliente conoce (`previousStreetBet`,
`villainAggressorCheckedPreviousStreet`, `villainBarreling`, `heroFloatedFlop`,
`villainBetSizeFlop`, `villainBetSizeTurn`, `villainCheckedMiddleStreet`,
`turnCalledWithFlushDanger`, `heroCheckedAllStreets`, `isAnyoneAllIn`,
`isDonkBet`). Del villano viajan `foldToBetPct`, `type` y
`adjustedFoldEquity`, que salen del tracking local.

**No** viajan `equity`, `potOdds`, `effectiveOuts`, `heroKickerStrength`, la
textura ni el `boardChange`: son función de las cartas y los calcula el servidor
(`UnifiedPokerCalculator`, `BoardTextureAnalyzer`,
`PreflopAnalyzer.CalculateCbetAdjustment`). Mandarlos precalculados dejaría el
motor en el cliente y anularía el propósito de licenciarlo. El servidor no
infiere estado que el cliente ya posee, pero tampoco confía en resultados que
puede calcular.

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

### `HttpDecisionEngine`

Implementa `IDecisionEngine`. `HttpClient` tipado con
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

Cuatro fases, cada una mergeable y reversible. Flag `RemoteEngine:Enabled` en el
cliente, apagado por defecto hasta la fase 4.

### Fase 1 — Caracterización

Antes de mover una línea se congelan fixtures generados **desde el código
actual**, no desde el diseño:

- Postflop: un test-generador recorre estados de mesa y guarda
  `DecisionResult` en `test/fixtures/decisions/postflop/`.
- Preflop: mismo mecanismo con `IRandomProvider` fijo, para que la mezcla
  ponderada sea reproducible.

Los fixtures se commitean y pasan a ser el contrato de paridad de todas las
fases siguientes.

Salida: suite de caracterización verde contra el código de hoy.

### Fase 2 — Motor local unificado

- Extraer `PostflopDecisionEngine` de `GameCoordinator` y hacer que el
  coordinator delegue conservando contexto, tracking, exploitability y logging.
- Mover preflop (`SetPreflopActionUseCase`, use cases, `UserHandHelper`,
  `GetActionScenario`) al motor, con `IPreflopRangeStore` inyectado.
- Mover `PlayerGameState` y compañía a `Domain`.
- Exponer `IDecisionEngine`/`LocalDecisionEngine` como puerta única y borrar
  `PokerDecisionFacade`.

Estado: completada. El coordinador delega en `PostflopDecisionEngine`; el
recableado para que consuma `IDecisionEngine` es parte de la fase 3a, porque
recién ahí existen dos implementaciones para intercambiar.

Salida: paridad 100% sobre los fixtures de la fase 1, sin cambios observables.

### Fase 3 — Servicio y cliente HTTP

Se parte en dos entregas mergeables.

**3a — Paridad.** Se crean `Contracts` y `OpenScrape.Api` con
`POST /v1/decisions` y `GET /health`, sin autenticación. Se recablea
`GameCoordinator` para llamar a `IDecisionEngine` y se agrega
`HttpDecisionEngine` detrás del flag `RemoteEngine:Enabled`, apagado por
defecto. Los 15 fixtures se replayed contra el host en proceso con RNG fijo.

Estado: completada (commit `c11d305`, 1330 tests verdes, paridad 15/15 por
HTTP). El preflop del cliente sigue llamando a `ISetPreflopActionUseCase`
porque el contrato no devuelve el estado mutado que usa la calle siguiente;
el camino preflop por HTTP queda cubierto por `ParidadHttpTests`.

Salida: el mismo `DecisionRequest` produce la misma decisión por el camino local
y por HTTP, en las cuatro calles.

**3b - Licencias.** Marten, JWT, cuotas, `LicenseCli` y `FormLicense`, según
`## Licencias y seguridad`. El endpoint pasa a exigir Bearer.

Salida: `/v1/decisions` rechaza sin licencia válida y respeta la cuota del plan.

Estado: completada (commit `a9d3448` de la rama `feature/api-engine-fase-3b`,
1379 tests verdes). La compuerta de licencia está en el arranque del cliente;
un token que vence a mitad de sesión obliga a reiniciar, no a reactivar en
caliente.

### Fase 4 — Retirar el motor del cliente

Se borran del cliente el motor y el `IDecisionEngine` local, se elimina el flag
y queda sólo `HttpDecisionEngine`. Se verifica el publish.

Salida: el artefacto publicado no contiene `OpenScrape.DecisionMaker.dll`.

Estado: **fase 4a completada** (rama local `feature/api-engine-fase-4a`,
commits `06e6eb6`→`3005c9d`). `OpenScrape.App` ya no referencia
`OpenScrape.DecisionMaker`, no instancia el motor local y decide siempre por
HTTP; el overlay, el resume y los logs se alimentan de `Contracts.DecisionResult`.
El gate `verify-pre-merge.ps1` publica el cliente y falla si aparece
`DecisionMaker.dll`. La fase 4b (sacar `OpenScrape.Features` del cliente) queda
pendiente, así que el publish todavía incluye `OpenScrape.Features.dll`.

## Pruebas

### Unitarias del motor

Las existentes se mantienen. `OpenScrape.App.Tests` ya referencia
`OpenScrape.DecisionMaker` de forma directa, así que los tests del motor siguen
compilando cuando `App` deje de referenciarlo. El test de arquitectura valida el
csproj de `App` y el publish, no el proyecto de tests.

### Caracterización y paridad

- Fixtures postflop: acción exacta, equity ±0,1, sizing ±0,1, `Reason` no vacío.
- Fixtures preflop: acción exacta con `IRandomProvider` fijo. Sin RNG fijo, la
  mezcla ponderada hace el test flaky y la paridad no se puede afirmar.
- En la fase 3a el replay corre contra `WebApplicationFactory`, con el
  `IRandomProvider` real reemplazado por uno determinista y la misma semilla que
  usó el fixture. En producción el servidor usa el RNG real.
- Un fixture que no coincide bloquea el merge.

### Contrato

Serialización y deserialización de cada DTO de `Contracts`, incluidos enums y
campos opcionales. Un cambio de forma rompe el test antes de romper al cliente.

### Integración de la API

`WebApplicationFactory` con `ILicenseStore` en memoria. Casos: activación
válida, key inexistente, token vencido, token con firma inválida, licencia
revocada, cuota excedida, request inválido, decisión imposible.

Marten se usa sólo en producción; los tests no requieren Postgres.

### Arquitectura

- `OpenScrape.Contracts` no referencia Marten ni ASP.NET. Se valida desde la
  fase 3a.
- Al terminar la fase 4a, `OpenScrape.App.csproj` no contiene `ProjectReference`
  a `DecisionMaker`. Durante 3a el camino local sigue vivo y la referencia es
  legítima.
- Al terminar la fase 4a, el directorio de publish del cliente no contiene
  `OpenScrape.DecisionMaker.dll`. La ausencia de `OpenScrape.Features.dll` es la
  salida de la fase 4b, todavía pendiente.

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
| Mover preflop cambia decisiones | El usuario pierde confianza en el bot | Fixtures de caracterización generados antes del movimiento, con RNG fijo |
| Movimiento de `PlayerGameState` a `Domain` | 147 call sites, errores de namespace | Cambio mecánico, verificado por el compilador; sin tocar lógica |
| Latencia de red en el turno | El bot decide tarde | Timeout 1,5 s, servidor en la misma región que el usuario, medir p95 |
| Caída del servicio | El bot se detiene | Monitoreo y reinicio automático; aceptado por diseño |
| Key compartida entre instalaciones | Pérdida de ingresos | Cuota por token y rate limit; device binding queda como fase futura |
| Deriva del contrato | Cliente y API incompatibles | `Contracts` compartido + tests de contrato + versión en la ruta |
| Secreto JWT filtrado | Tokens falsificables | Variable de entorno, rotación, gitleaks ya activo en CI |
| Monte Carlo en el servidor | El cálculo de equity consume el turno | Medir p95; iteraciones por request configurables y ajustadas contra el timeout de 1,5 s |

## Criterios de aceptación

1. Los fixtures de caracterización pasan 100% al terminar la fase 2.
2. `OpenScrape.Api` responde `/v1/decisions` con paridad sobre preflop y
   postflop.
3. En la fase 3a, los 15 fixtures dan paridad exacta por el camino local y por
   el HTTP. Al terminar la fase 4, el publish del cliente no incluye
   `OpenScrape.DecisionMaker.dll` ni `OpenScrape.Features.dll`.
4. Suite completa en verde y 0 advertencias en Release.
5. `dotnet format --verify-no-changes` sin cambios.
6. `verify-pre-merge.ps1 -SkipSmoke` con todos los checkpoints en PASS.
