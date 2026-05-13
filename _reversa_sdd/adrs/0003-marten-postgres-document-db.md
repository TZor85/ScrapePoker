# ADR-0003 — Marten + PostgreSQL como persistencia (Document DB sobre relacional)

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha (inferida):** ~2026-03-15 (commit `2360036 refactor(core): Remove ML.NET, update config & logging` confirma upgrade a Marten 8.24)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto

## Contexto

El sistema necesita persistir:

- Sesiones de juego (con metadatos por mesa).
- Manos individuales con cartas, decisiones por calle, telemetría.
- Mapas de regiones por sala (configuración mutable, JSON estructurado).
- Cards (cache de imágenes de cartas).
- Strategy profiles (futuro).
- Licencias (futuro).

El esquema es **flexible** (la estructura de `HandRecord` ha cambiado al menos 4 veces en commits — añadiendo `BlindPosted`, `AutoRebuy`, `Telemetry`). Persistencia relacional con migrations sería costosa para evolución frecuente. El flujo de queries es simple: por sesión, por mano, por jugador.

## Decisión

Usar **Marten** (Jasper FX) sobre **PostgreSQL** como document store. Cada entidad tiene su tipo `T` y se persiste vía `IDocumentSession`. Las queries son LINQ sobre `IDocumentSession.Query<T>()`. Marten genera schema en JSONB con índices secundarios.

Configuración en `OpenScrape.Infrastructure/Services.cs`. Connection string en `appsettings.Development.json` (gitignored) — apunta a Neon DB en la nube.

## Alternativas consideradas

1. **EF Core con PostgreSQL relacional.** Considerado pero rechazado. Razones:
   - Migrations cada vez que cambia `HandRecord` (alta frecuencia).
   - El JSON `List<StreetDecision>` requiere tabla auxiliar + N+1 queries o `OWN_JSON`. Marten lo guarda como JSONB nativo.
   - El esquema "una sesión = un agregado con muchas manos" se modela mejor como documento.
2. **MongoDB.** Rechazado. El proyecto ya usa PostgreSQL para futuras tablas relacionales (`License`, futuras métricas agregadas) — añadir Mongo duplica infra.
3. **SQLite local.** Considerado para una versión "offline only". Rechazado porque las sesiones deben sincronizarse entre máquinas (un usuario juega en varias) y porque la spec de licencias requiere BD compartida.
4. **Archivos JSON en disco.** Era el estado original (`Data/*.json` aún se usa para tablas preflop). Rechazado para sesiones porque escalar a >10K manos sería I/O-pesado y no permite queries.
5. **Redis.** Rechazado: persistencia volátil; las manos son histórico durable.

## Consecuencias

**Positivas:**

- Schema-less: cambiar `HandRecord` no requiere migration. Marten reescribe documentos al primer fetch (lazy migration).
- Queries simples: `session.Query<HandRecord>().Where(h => h.GameSessionId == id).ToListAsync()`.
- Connection pool integrado, transacciones por sesión.
- PostgreSQL JSONB indexable: se puede hacer `CREATE INDEX ON hand_records ((data->>'GameSessionId'))`.
- Backup/restore con `pg_dump` estándar.
- Marten 8.24 trae `IAsyncDisposable` y compatibilidad con DI moderno.

**Negativas:**

- **Marten es .NET only.** Migrar a stack distinta requiere reimplementar el access layer.
- Connection string hardcoded en `appsettings.json` actualmente (anomalía Scout). Si una persona obtiene el binario, accede a la BD compartida. **Riesgo activo.**
- Sin migrations explícitas → cambios de schema "rompen silenciosamente" en lecturas viejas si no se versionan los documentos. Mitigado parcialmente por reglas como `HandRecord.Telemetry?` nullable (commit `975bfaa feat(domain): HandRecord.Telemetry opcional para agregados por mano`).
- Queries complejas (joins entre sesiones y manos para `StrategyAnalyzerService`) requieren múltiples roundtrips. **Anomalía detectada en `_reversa_sdd/code-analysis.md`:** N+1 queries en `BankrollTrackerService`.

**Implicaciones para una migración:**

- Si se cambia a EF Core, hay que diseñar tablas `game_sessions` + `hand_records` con FK + `street_decisions` (tabla aux) + `card_cache`. ~6 migrations iniciales.
- Si se mueve a Mongo, queda casi 1:1 con BSON.
- Si la app va multi-tenant (post `login-sistema-licencias`), añadir `OwnerLicenseKey` a cada documento y filtrar siempre por él. **Cambio breaking.**

## Referencias

- `src/OpenScrape.Infrastructure/Services.cs` — setup Marten.
- Commit `2360036` — bump Marten 8.24.0.
- Commit `eaf5643 refactor(core): Fix memory leaks in OCR and DB sessions` — refactor a `await using IDocumentSession` per-operation (no long-lived sessions).
- Commit `ec0e19f refactor/persistence: migrate to GameSession/HandRecord model` — separación documento sesión vs documento mano.
- Anomalía Scout: `appsettings.json` con connection string Neon real.
