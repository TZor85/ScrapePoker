# ADR-0020 — OpenSpec spec-driven development con `openspec/changes/`

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** ~2026-03-25 (commit `2e6e5d4 [chore/docs]: Archive completed specs (decision-engine-v2, bankroll-dashboard)`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto

## Contexto

El motor de decisión cambia con cierta frecuencia (~17 specs archivadas + 4 abiertas). Cada cambio toca múltiples archivos en múltiples capas (Domain + DecisionMaker + App + Tests). Sin documentación de **por qué se introdujo cada cambio** y **qué se esperaba que cumpliera**, el código se vuelve críptico para futuro yo / nuevos contributors.

Adicionalmente, los commits a veces traen >20 archivos modificados — el commit message no captura todo el contexto.

## Decisión

Adoptar **OpenSpec / opsx** como framework de spec-driven development:

1. **Carpeta `openspec/`** en la raíz del repo:
   - `openspec/changes/` — specs propuestos / en progreso (4 actualmente: `decision-engine-v3`, `fix-player-detection-p5`, `login-sistema-licencias`, `telemetry-quality-checkpoints`).
   - `openspec/changes/archive/` — specs archivados tras implementación (17 archivados).
   - `openspec/specs/` — capabilities promovidas tras archive (≥5 capabilities documentadas: `postflop-context-holder`, `postflop-decision-api`, `decision-matrix-coverage`, `textbox-logger-sink`, `thresholds-registry`, `strategy-profile-validation`).
2. **Estructura por change:**
   - `proposal.md` — Why, What Changes, Capabilities (New/Modified), Impact (archivos afectados, dependencias).
   - `design.md` — Layout, data flow, DTOs, decisiones técnicas.
   - `tasks.md` — Pasos numerados de implementación.
   - `specs/{capability}/spec.md` — Requisitos en formato Gherkin-light (BDD): `Requirement: …` + `Scenario: WHEN…THEN…`.
3. **Slash commands:**
   - `/openspec-propose` — crear nueva spec.
   - `/openspec-explore` — modo brainstorm.
   - `/openspec-apply-change` — implementar tasks.
   - `/openspec-archive-change` — archivar tras completar.
4. **`opsx`** (variante experimental) coexiste — separación entre flujos estables y experimentales.

## Alternativas consideradas

1. **Sin specs — solo commit messages.** Rechazado: los commits no narran *por qué* y se pierde contexto.
2. **ADRs propios** (Architecture Decision Records). Rechazado como **único** mecanismo: ADRs son ideales para decisiones macro (las que documenta este Reversa). OpenSpec cubre cambios de feature granulares con tareas y scenarios.
3. **GitHub Issues + labels.** Rechazado: GH issues no se versionan en el repo, son separados del código y se pierden si el repo se mueve.
4. **Notion / Confluence.** Rechazado: vincular specs al código requiere context-switching constante.
5. **Linear / Jira.** Rechazado para proyecto unipersonal — overhead.
6. **Markdown en `docs/`** sin estructura formal. Estado parcial — `docs/` existe (`pre-merge-checklist.md`). Rechazado como reemplazo: la estructura formal de OpenSpec ayuda a no olvidar pasos.

## Consecuencias

**Positivas:**

- **Cada cambio tiene rastro narrativo:** propósito, diseño, tareas, criterios de aceptación.
- **Specs archivadas son patrimonio**: 17 archivos detallando refactorings y bugfixes históricos. **Especialmente valiosos para el Reversa** que está leyendo este texto ahora mismo: muchas decisiones de los ADR-0007, ADR-0008, ADR-0009 vienen de archivos OpenSpec.
- **Capabilities promovidas** (`openspec/specs/`) son la **especificación viva** de funcionalidades — más estable que código.
- **Slash commands** estandarizan flujo: `/openspec-propose → /openspec-apply-change → /openspec-archive-change`.
- **Coexiste con git workflow** (feature branches con specs en el branch).

**Negativas:**

- **Overhead de mantenimiento:** cada cambio relevante demanda 4 archivos. Para fixes pequeños es exagerado — y muchos commits del proyecto NO tienen spec asociada.
- **Inconsistencia:** algunos refactors tienen spec (`refactor-frmmain-coordinators`), otros no (`refactor/storage: unify folder name`). Sin política clara de cuándo se exige spec.
- **Dos sistemas paralelos** (`openspec/changes/` y `openspec/changes/archive/`) — riesgo de olvidar mover y dejar specs `completos pero no archivados`.
- **Lecturas largas:** una spec con 4 archivos puede tener 200+ líneas, fricción para revisar PRs.

**Implicaciones para una migración:**

- En cualquier herramienta de spec management (Linear, GH issues con templates, Architecture Doctor) el patrón **proposal + design + tasks + scenarios** es replicable.
- Los archivos archivados son **input directo** del Reversa Architect (próximo agente) para construir Spec Impact Matrix.

## Referencias

- `openspec/changes/` y `openspec/changes/archive/`.
- `openspec/specs/` — capabilities canonical.
- `openspec/config.yaml` — configuración del framework.
- Slash commands en `.claude/commands/openspec-*.md`.
- Commit `2e6e5d4 [chore/docs]: Archive completed specs`.
