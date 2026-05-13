# ADR-0018 — Configuración por ambiente: `appsettings.Development.json` gitignored, `appsettings.json` con placeholders

- **Estado:** 🟡 ACEPTADO PERO INCOMPLETO (anomalía detectada por Scout)
- **Fecha:** ~2026-03-16 (commit `2360036 refactor(core): Remove ML.NET, update config & logging`)
- **Confianza:** 🟢 CONFIRMADO en CLAUDE.md, 🔴 LACUNA en estado actual del archivo
- **Decisor inferido:** Alberto

## Contexto

El proyecto necesita parámetros sensibles:

- Connection string de PostgreSQL (Neon DB en cloud).
- `Encrypter.Key` (clave AES de 32 bytes para ofuscar screenshots).
- Eventualmente: `AdminLicense:Key` cuando se implemente el sistema de licencias.

Antes del refactor `2360036`, estos valores estaban directamente en `appsettings.json` y se commitearon al repo. **Cualquiera con acceso al historial git tenía acceso a la BD.**

## Decisión

1. **`appsettings.json`** (committed):
   - Estructura completa con todos los parámetros y secciones.
   - Valores de secretos = placeholders `CHANGE_ME`. Documentado en CLAUDE.md.
   - Strategy thresholds, parámetros de juego: valores reales (no son secretos).
2. **`appsettings.Development.json`** (gitignored):
   - Override con secretos reales.
   - **Nunca commitear.**
3. **Variable de entorno `DOTNET_ENVIRONMENT`**:
   - Default: `"Development"` (definido por `Program.Main` al arrancar — único caso donde Development es el default por design, justificado por ser app desktop).
   - `launchSettings.json` lo configura para Visual Studio.
4. **`appsettings.Production.json`** (futuro, opcional): override para builds de release.
5. **`.gitignore`** incluye `appsettings.Development.json`.

## Alternativas consideradas

1. **Vault / Azure Key Vault.** Rechazado por overhead — app desktop sin orchestador.
2. **User Secrets (`dotnet user-secrets`).** Atractivo. Rechazado porque `User Secrets` es DEV-only y vincula a un user de Windows; el binario distribuido no lo usa.
3. **Variables de entorno directas.** Rechazado: el usuario casual no las configura. `appsettings.Development.json` es más friendly.
4. **Cifrar `appsettings.json` con Windows DPAPI.** Considerado para futuro. Rechazado por ahora — añade fricción para edición manual.
5. **Backend de configuración externo.** Rechazado por dependencia de red al arrancar.

## Consecuencias

**Positivas:**

- **Secretos no en historial git** (al menos en teoría — ver §Anomalía).
- Estructura completa visible en `appsettings.json` — onboarding rápido.
- `DOTNET_ENVIRONMENT` permite build releases con configuración distinta.
- Compatible con `Microsoft.Extensions.Configuration` estándar.

**Negativas — anomalía actual 🔴:**

> **El Scout detectó (anomalía `secret_in_committed_config`) que `appsettings.json` actualmente contiene una connection string PostgreSQL real (Neon DB) y una `Encrypter.Key` real**. CLAUDE.md declara que deberían ser `CHANGE_ME`, pero el estado del archivo no se ajusta.

Posibles causas:

- El refactor `2360036` no rotó las credenciales después de moverlas de `appsettings.json` a `Development.json` y por error se reintrodujeron.
- Edición posterior accidental.
- Decisión consciente: "los usuarios reciben el binario con conn string ya rellenada para no tener que configurar".

**Riesgos derivados:**

- Cualquier persona con el binario (o con acceso al repo) tiene acceso a la BD compartida.
- En multi-tenant futuro (post-licencias), un usuario malicioso puede leer datos de otros.

**Otras negativas:**

- Default `DOTNET_ENVIRONMENT=Development` puede sorprender a quien viene de ASP.NET Core (donde el default es Production). Documentado en CLAUDE.md.
- Sin un sistema de validación al arrancar, valores `CHANGE_ME` pueden pasar desapercibidos hasta que se intenta conectar a Marten y falla con "Authentication failed".

## Recomendaciones

🔴 **Urgente:**

1. Rotar la connection string PostgreSQL del Neon DB.
2. Rotar la `Encrypter.Key`.
3. Reemplazar valores en `appsettings.json` por `CHANGE_ME`.
4. Mover los reales a `appsettings.Development.json` (gitignored) — re-validar `.gitignore`.
5. Añadir validación al arrancar que rechace `CHANGE_ME` con mensaje claro: "configura `appsettings.Development.json`".

## Implicaciones para una migración

- Cualquier reimplementación debe **rotar** las credenciales antes del primer release.
- Si se mueve a SaaS, mover los secretos a Key Vault / AWS Secrets Manager / etc.
- El patrón `[base].json + [base].{ENV}.json` está soportado en casi todos los frameworks (Spring profiles, Node `config`, Rust `config-rs`).

## Referencias

- CLAUDE.md sección "Configuration & Secrets".
- Commit `2360036 refactor(core): Remove ML.NET, update config & logging` — primer move a Development.
- Anomalía Scout: `surface.json:anomalies[1]` `secret_in_committed_config`.
- `src/OpenScrape.App/Properties/launchSettings.json` — `DOTNET_ENVIRONMENT=Development`.
- Q-PERM-CRED-01..03 en `permissions.md`.
