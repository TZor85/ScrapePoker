# Architecture Decision Records — ScrapePoker

> ADRs retroactivos generados por el Detective del Reversa el 2026-05-06.
>
> Los ADRs documentan decisiones arquitectónicas inferidas del código y del histórico Git. Cada ADR incluye **Contexto, Decisión, Alternativas consideradas, Consecuencias** y **Referencias**.
>
> **Escala de confianza:** 🟢 CONFIRMADO · 🟡 INFERIDO · 🔴 LACUNA.

## Índice

| Nº | Título | Estado | Fecha |
|---|---|---|---|
| [0001](0001-clean-architecture-cinco-capas.md) | Clean Architecture en cinco capas | 🟢 ACEPTADO | ~2026-03-15 |
| [0002](0002-winforms-net10-windows.md) | WinForms sobre `net10.0-windows` como UI | 🟢 ACEPTADO | desde origen |
| [0003](0003-marten-postgres-document-db.md) | Marten + PostgreSQL como persistencia | 🟢 ACEPTADO | ~2026-03-15 |
| [0004](0004-ocr-en-vez-de-api-cliente-poker.md) | Scraping pasivo (OCR + visión) en vez de integración API | 🟢 ACEPTADO | foundational |
| [0005](0005-eliminar-ml-net-volver-determinismo.md) | Eliminar ML.NET y volver a motor determinístico | 🟢 ACEPTADO (reemplaza) | 2026-03-16 |
| [0006](0006-pipeline-unificado-equity-decision.md) | Pipeline unificado de equity y decisión postflop | 🟢 ACEPTADO | ~2026-03-16 |
| [0007](0007-postflop-context-inmutable-holder-scoped.md) | `PostflopGameContext` inmutable con holder scoped | 🟢 ACEPTADO (reemplaza) | 2026-04-21 |
| [0008](0008-thresholds-tipados-startup-validation.md) | `ThresholdsRegistry` tipado con startup validation | 🟢 ACEPTADO (reemplaza) | 2026-04-21 |
| [0009](0009-ilogger-textbox-sink-scopes.md) | `Microsoft.Extensions.Logging` con sink TextBox y scopes | 🟢 ACEPTADO (reemplaza) | 2026-04-21 |
| [0010](0010-monte-carlo-hibrido-enumeracion-exacta.md) | Monte Carlo híbrido (exact turn/river, MC adaptativo) | 🟢 ACEPTADO | ~2026-03-26 |
| [0011](0011-opponent-tracker-laplace-reliability.md) | `OpponentTracker` con reliability granular y Laplace | 🟢 ACEPTADO | ~2026-03-25 |
| [0012](0012-game-loop-state-machine-board-card-validation.md) | `GameLoopStateMachine` con validación por board cards | 🟢 ACEPTADO | 2026-04-02 |
| [0013](0013-auto-rebuy-detection-50bb-threshold.md) | Detección de auto-rebuy (50 BB threshold) | 🟢 ACEPTADO | ~2026-03-22 |
| [0014](0014-frmmain-scoped-no-root.md) | `FrmMain` resuelto desde scope (no root) | 🟢 ACEPTADO | ~2026-03-19 |
| [0015](0015-decision-matrix-216-cases-integration-test.md) | `DecisionMatrix` 216 casos como red de seguridad | 🟢 ACEPTADO | 2026-04-20 |
| [0016](0016-perf-pixel-sampling-region-card-cache.md) | Perf: LockBits, region cache, card cache | 🟢 ACEPTADO | ~2026-03-26 |
| [0017](0017-telemetria-metrics-collector-quality-checkpoints.md) | Telemetría con `IMetricsCollector` y pre-merge checkpoints | 🟢 ACEPTADO | 2026-04-27..30 |
| [0018](0018-config-environment-development-secrets-gitignored.md) | Configuración por ambiente, secretos en `Development.json` | 🟡 ACEPTADO PERO INCOMPLETO | ~2026-03-16 |
| [0019](0019-positions-moving-blinds-sitout-empty.md) | Posiciones con moving blinds (SitOut/Empty) | 🟢 ACEPTADO | 2026-04-15 |
| [0020](0020-openspec-spec-driven-development.md) | OpenSpec spec-driven development | 🟢 ACEPTADO | ~2026-03-25 |

## Áreas de decisión cubiertas

- **Estructura del proyecto:** ADR-0001 (capas), ADR-0014 (DI scope).
- **Stack tecnológico:** ADR-0002 (UI), ADR-0003 (persistencia), ADR-0009 (logging).
- **Filosofía del producto:** ADR-0004 (scraping pasivo), ADR-0005 (determinismo).
- **Motor de decisión:** ADR-0006 (pipeline), ADR-0010 (MC híbrido), ADR-0011 (oponente).
- **Estado y consistencia:** ADR-0007 (PostflopContext), ADR-0008 (Thresholds), ADR-0012 (FSM cards), ADR-0013 (auto-rebuy).
- **Testing y calidad:** ADR-0015 (DecisionMatrix), ADR-0017 (telemetría + checkpoints).
- **Performance:** ADR-0016.
- **Configuración / seguridad:** ADR-0018.
- **Reglas del juego:** ADR-0019 (moving blinds).
- **Workflow de cambios:** ADR-0020 (OpenSpec).

## Decisiones pendientes / abiertas

Estas merecen ADR cuando se implementen:

- 🔴 **ADR-future-1:** Sistema de licencias (`openspec/changes/login-sistema-licencias/`). Spec lista; código no.
- 🔴 **ADR-future-2:** Estrategia de persistencia de `OpponentProfile` cross-sesión (ver Q-FSM-02 en `state-machines.md`).
- 🔴 **ADR-future-3:** Decision Engine V3 (`openspec/changes/decision-engine-v3/`). Spec abierta.

## Cómo usar estos ADRs

- **Para una migración** (vía `/reversa-migrate`): cada ADR enumera "Implicaciones para una migración" — punto de partida del paradigm advisor.
- **Para refactor en curso:** consultar el ADR relacionado para entender por qué se decidió X. Si la decisión se invalida, **proponer un ADR de reemplazo**, no editar el viejo.
- **Para onboarding:** leer en orden 0001 → 0006 → 0009 da un overview de la arquitectura. Los demás se pueden leer al tocar el área.

---

> Generado por `reversa-detective` — `_reversa_sdd/adrs/`.
