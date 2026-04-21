## Context

El sistema actual tiene **dos** copias del mismo patrón frágil, ambas silenciosas ante errores de configuración:

1. `PostflopDecisionService.GetThresholds(BoardPosition, HandSituation)` — líneas 38–58. Construye clave string `"{street}_{situation}"`, consulta `_profile.Thresholds` (un `Dictionary<string, StreetThresholds>` poblado desde `appsettings.json`), y en ausencia de la clave devuelve un fallback hardcoded con un `Console.WriteLine` informativo. El overlay ya no muestra Console y el fallback es suficientemente "razonable" como para que el bot siga jugando — mal — sin pintar error visible.

2. `StrategyProfileService.GetThresholds(BoardPosition, HandSituation)` — líneas 27–45. Mismo patrón, sin siquiera el `Console.WriteLine`. Se usa principalmente desde el overlay/diagnóstico para pintar tiers en pantalla.

El `appsettings.json` contiene ~30 entradas tipo `"Flop_OpenRaise"`, `"Turn_ThreeBet"`, `"River_Squeeze"`. Los enums `BoardPosition` y `HandSituation` viven en `OpenScrape.Domain.Enums.Positions`. Si alguien añade `HandSituation.VsSqueeze` y olvida las 3 entradas `Flop|Turn|River_VsSqueeze`, las decisiones en ese flujo caen al fallback y nadie se entera. Si alguien escribe `"Fop_OpenRaise"` por typo, idem.

Stakeholders: el desarrollador único (Alberto), que necesita confianza de que la configuración está completa antes de abrir una sesión de poker, no un warning perdido en Console.

## Goals / Non-Goals

**Goals:**

- Reemplazar la clave string por un value object tipado `ThresholdKey(BoardPosition, HandSituation)` que sólo permite combinaciones válidas postflop.
- Centralizar el acceso a thresholds en un servicio `IThresholdsRegistry` singleton, eliminando las dos copias del fallback.
- Ejecutar una validación **fail-fast** al arrancar la app: claves inválidas, combinaciones obligatorias ausentes o tiers incoherentes abortan el proceso con mensaje explícito.
- Conservar 100% de compatibilidad con `appsettings.json` actual (las claves string siguen siendo el formato de configuración; sólo cambia cómo se validan y consumen).
- No introducir regresiones en las ~638 pruebas existentes.

**Non-Goals:**

- Cambiar el esquema de `appsettings.json` o migrar a YAML/TOML.
- Rediseñar `StreetThresholds` o añadir nuevos campos.
- Convertir la configuración en hot-reload (sigue siendo estática al arrancar).
- Cambiar cómo preflop consume `StrategyProfile` (este trabajo es puramente postflop).
- Reemplazar `Dictionary<string, ...>` en `StrategyProfile` por `Dictionary<ThresholdKey, ...>` — el binding de `IOptions<T>` desde JSON requiere claves string, así que `StrategyProfile` mantiene el diccionario string crudo y `ThresholdsRegistry` lo traduce.

## Decisions

### 1. `ThresholdKey` como record tipado (en Domain)

**Decisión:** Crear `public record ThresholdKey(BoardPosition Street, HandSituation Situation)` en `OpenScrape.Domain.ValueObjects`. El constructor rechaza `Street ∈ {None, Hand}` y `Situation = None`. `Situation = Call` sí es válido: representa al jugador que llamó preflop y afronta postflop; `appsettings.json` define `Flop_Call`, `Turn_Call`, `River_Call`. `ToString()` devuelve `"{Street}_{Situation}"`.

**Alternativas consideradas:**

- *Enum plano combinado (`ThresholdKeyEnum.Flop_OpenRaise = 1, ...`)*: Descartado. La explosión combinatoria (3 streets × 12+ situaciones = 36+ valores) es mantenible, pero pierde la composición y duplica información ya presente en los enums fuente.
- *`readonly struct`*: Equivalente funcional al record, pero perdemos el `Equals`/`GetHashCode` y el deconstruct gratuitos. El record es más ergonómico.
- *Generar código con un source generator desde los enums*: Overkill para 36 combinaciones.

**Rationale:** El record con validación en el constructor es idiomático en .NET 10, zero-alloc efectivo para igualdad, y aprovecha la información que ya vive en los enums del dominio.

### 2. `IThresholdsRegistry` singleton, construido desde `IOptions<StrategyProfile>`

**Decisión:** Crear `IThresholdsRegistry` en `OpenScrape.DecisionMaker.Interfaces` con `Get`, `TryGet`, `Contains`, `Keys`. La implementación `ThresholdsRegistry` se registra como singleton dentro de `AddDecisionMaker`. En su constructor, parsea cada clave string del perfil a `ThresholdKey` (string → split por `_` → `Enum.Parse`) y construye un `Dictionary<ThresholdKey, StreetThresholds>` interno.

**Alternativas consideradas:**

- *Hacer que `StrategyProfile` expusiera directamente `Dictionary<ThresholdKey, StreetThresholds>`*: Imposible sin un `IConfiguration` custom binder. `IOptions<T>` + JSON binding sólo soporta claves string con tipos primitivos.
- *Mantener el diccionario string y wrappearlo con un helper estático*: No permite inyección en tests ni garantiza que el parseo ocurra una sola vez.
- *Servicio scoped*: Innecesario; los thresholds son inmutables durante la vida del proceso.

**Rationale:** El singleton garantiza parseo único, lookup O(1), y testabilidad completa por DI.

### 3. Fail-fast en `Program.cs` con `StrategyProfileValidator`

**Decisión:** Crear `StrategyProfileValidator` como clase estática con un único método `Validate(StrategyProfile profile)` que lanza `StrategyProfileValidationException` (nueva excepción de dominio) con mensaje acumulado de todos los errores encontrados. Invocarlo en `Program.cs` inmediatamente después de `var host = builder.Build();` y antes de resolver `FrmMain`. Si lanza, mostrar un `MessageBox` con el mensaje (es una WinForms app) y salir con `Environment.Exit(1)`.

**Alternativas consideradas:**

- *`IHostedService` con `StartAsync`*: Sobre-ingeniería para lo que es una validación sincrónica instantánea. El host builder de WinForms no ejecuta hosted services automáticamente en esta app (el game loop es manual).
- *`IValidateOptions<StrategyProfile>` de `Microsoft.Extensions.Options`*: Elegante, pero se dispara *la primera vez* que se resuelve `IOptions<StrategyProfile>`, que en esta app ocurre de forma dispersa. Queremos fallar antes de mostrar `FrmMain`, no cuando se construye el primer servicio que consume el perfil.
- *Validador en el constructor del `ThresholdsRegistry`*: Daría fail-fast pero el mensaje llegaría más tarde (cuando DI resuelve el registry por primera vez) y no permitiría reportar errores de tiers incoherentes en claves que no se consultan.

**Rationale:** La invocación explícita en `Program.cs` es trivial, visible, y permite mostrar un `MessageBox` con el error — mucho mejor UX que un crash silencioso.

### 4. `StrategyProfile.Thresholds` sigue siendo `Dictionary<string, StreetThresholds>`

**Decisión:** No tocar el shape de `StrategyProfile`. Marcarlo (comentario) como "fuente cruda de configuración; consumir vía `IThresholdsRegistry`". Eliminar `StrategyProfileService.GetThresholds`; quien lo usa pasa a inyectar `IThresholdsRegistry`.

**Alternativas consideradas:**

- *Hacer `StrategyProfile.Thresholds` `internal` para forzar el uso del registry*: Rompería el binding de `IOptions`. Alternativa válida: `[JsonInclude]` con setter privado. Descartado por complejidad marginal.

**Rationale:** La superficie pública del registry es la nueva API; el diccionario crudo queda como detalle de carga.

### 5. Lista obligatoria de combinaciones

**Decisión:** `StrategyProfileValidator` mantiene una lista estática de combinaciones `(Street, Situation)` obligatorias. Incluye las 36 combinaciones `{Flop, Turn, River} × {OpenRaise, Call, RaiseOverLimper, ThreeBet, OpenRaiseVs3Bet, OpenRaiseVs3BetAndCall, FourBet, Cold4Bet, Squeeze, VsSqueeze, DonkBet, DonkBetVsOpenRaise}`.

**Hecho comprobado durante la exploración:** `appsettings.json` ya cubre exactamente estas 36 combinaciones. El validador puede activarse en modo estricto desde el primer commit sin requerir completar datos previamente. `LimpRaise` queda fuera de la lista obligatoria porque no aparece en el JSON actual; si en el futuro se decide que es obligatorio, se añadirá con un change posterior.

## Risks / Trade-offs

- **Riesgo:** el `appsettings.json` actual puede no cubrir las 36 combinaciones obligatorias; activar el validador rompería el arranque. → **Mitigación:** auditar como primer paso de `tasks.md`; añadir las entradas faltantes con tiers clonados de la situación más similar antes de activar la validación estricta.
- **Riesgo:** consumidores actuales de `StrategyProfileService.GetThresholds` (overlay, diagnóstico) dejan de compilar. → **Mitigación:** grep exhaustivo de call-sites en `tasks.md`; migrar cada uno a `IThresholdsRegistry`.
- **Riesgo:** tests existentes que construyen `StrategyProfile` con pocos thresholds y pasan por el fallback silencioso romperán. → **Mitigación:** esos tests probablemente probaban lógica de decisión, no la configuración; dotarlos de un `ThresholdsRegistry` de test con las claves mínimas necesarias (factory helper `TestThresholdsRegistry.WithDefaults()`).
- **Trade-off:** introducir una excepción nueva (`StrategyProfileValidationException`) aumenta la superficie de dominio. Aceptable: el nombre es autoexplicativo y el uso está confinado al arranque.
- **Riesgo:** el constructor de `ThresholdsRegistry` lanza si una clave string del perfil no parsea, pero `StrategyProfileValidator` ya debería haber detectado ese caso. Si el orden de llamada falla (registry resuelto antes del validador), el error se presenta como `InvalidOperationException` genérico en lugar del mensaje amigable. → **Mitigación:** documentar en `Program.cs` el orden; test que verifica que la validación corre antes de cualquier resolución de `IThresholdsRegistry`.

## Migration Plan

1. **Paso 1 — Auditoría de `appsettings.json`:** listar qué claves `{Street}_{Situation}` existen y qué combinaciones de la lista obligatoria faltan. Añadirlas con tiers clonados del match más cercano. Commit separado.
2. **Paso 2 — Añadir `ThresholdKey` y tests:** introducir el record en `Domain`; no cambia nada más.
3. **Paso 3 — Añadir `IThresholdsRegistry` y `ThresholdsRegistry`:** implementar, registrar en `AddDecisionMaker`, tests unitarios.
4. **Paso 4 — Añadir `StrategyProfileValidator` y `StrategyProfileValidationException`:** implementar, tests unitarios cubriendo cada caso de error.
5. **Paso 5 — Integrar en `Program.cs`:** llamar al validador tras `builder.Build()`. Probar arranque manual.
6. **Paso 6 — Migrar `PostflopDecisionService`:** inyectar `IThresholdsRegistry`, reemplazar `GetThresholds` público, eliminar fallback, ejecutar suite completa.
7. **Paso 7 — Migrar `StrategyProfileService`:** eliminar `GetThresholds`, migrar consumers.
8. **Paso 8 — Verificación final:** `dotnet build`, `dotnet test`, arranque manual de la app, abrir overlay y comprobar que todos los tiers se pintan.

**Rollback:** cada paso es un commit independiente. Revertir el PR restaura el comportamiento anterior.

## Open Questions

- *(Resuelto)* ¿Cuántas combinaciones faltan en `appsettings.json`? Ninguna: están las 36 `{Flop, Turn, River} × {OpenRaise, Call, RaiseOverLimper, ThreeBet, OpenRaiseVs3Bet, OpenRaiseVs3BetAndCall, FourBet, Cold4Bet, Squeeze, VsSqueeze, DonkBet, DonkBetVsOpenRaise}`. El validador puede activarse en modo estricto desde el primer commit.
- ¿El validador debe correr también cuando se hot-reload `appsettings.json` durante desarrollo? Fuera de scope; se mantiene sólo al arranque.
- ¿Cómo migrar los tests existentes que validan el fallback silencioso (`GetThresholds_KeyInexistente_DeberiaRetornarFallback` en `StrategyProfileTests` y `PostflopDecisionServiceTests`)? Respuesta: convertir a aserciones de que `Get` lanza `KeyNotFoundException` y `TryGet` retorna `false`. Tareas añadidas en `tasks.md` §7 y §3.
