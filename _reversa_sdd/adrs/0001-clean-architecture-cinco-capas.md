# ADR-0001 — Clean Architecture en cinco capas (.NET 10)

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha (inferida):** ~2026-03-15 (commit `340d664 feat(decision-maker): Add new project and enhance logic` separa motor de decisión de App)
- **Confianza:** 🟢 CONFIRMADO en código y estructura de solución
- **Decisor inferido:** Alberto (único contributor)

## Contexto

El proyecto comenzó como aplicación WinForms monolítica (`OpenScrape.App`) que mezclaba captura de pantalla, OCR, lógica de juego y persistencia. Los commits prehistóricos del repo (`Initial commit`, `Initial project`, `Cards flop`, `Modify cash games`) muestran un solo `.csproj` haciendo todo. A medida que crecieron las reglas de negocio del motor (equity, outs, hand evaluation), apareció necesidad de separar dominio puro de la UI.

## Decisión

Adoptar Clean Architecture con cinco proyectos en una solución única (`OpenScrape.sln`):

1. **`OpenScrape.Domain`** — entidades, value objects, enums, mappers. Sin dependencias externas.
2. **`OpenScrape.Features`** — casos de uso scoped agrupados por feature folder (`Table/`, `Cards/`, `ActionScenario/`, `RegionsTableMap/`, `GameRound/`). Usa `Ardalis.Result` como tipo de retorno.
3. **`OpenScrape.Infrastructure`** — Marten + PostgreSQL setup (`Services.cs` configura el document store).
4. **`OpenScrape.DecisionMaker`** — algoritmos y motor de decisión. Independiente de la UI.
5. **`OpenScrape.App`** — WinForms + composition root + servicios concretos (OCR, screen capture).

Las dependencias fluyen `App → Features → Domain` y `App → DecisionMaker → Domain`. `Infrastructure` solo lo referencia `App` para registrar Marten en DI.

## Alternativas consideradas

1. **Mantener monolito en `OpenScrape.App`.** Era el estado original. Rechazado porque hacía imposible testear el motor de decisión sin cargar WinForms — los tests del Decision Engine necesitan ser deterministas y rápidos.
2. **MediatR pipeline para casos de uso.** Considerado implícitamente (la convención `UseCases` y `IRequest` lo sugieren). Rechazado en favor de inyección directa de casos de uso scoped (CLAUDE.md confirma "scoped use cases (no MediatR pipeline)"). Razón: simplicidad, evitar overhead de pipeline para un app desktop sin web stack.
3. **Hexagonal / Ports & Adapters puro.** Demasiado ceremonioso para un app sin múltiples adaptadores externos. La estructura adoptada es Clean "ligera": Features hace de capa Application sin la verbosidad clásica.
4. **Dos proyectos solo (Domain + App).** Insuficiente: la lógica del motor (`DecisionMaker`) es demasiado grande (~3K LOC) y semánticamente distinta a casos de uso CRUD.

## Consecuencias

**Positivas:**

- Tests del motor (645+ NUnit tests) corren sin WinForms.
- `OpenScrape.DecisionMaker` es reutilizable en otros frontends (ej: web admin, CLI backtest).
- Inversión de control clara: la UI consume servicios; los servicios no conocen la UI (excepto el sink `TextBoxLogger` que recibe el TextBox por DI).
- Refactor de `FrmMain` (5,977 → 4,244 LOC; commit `565241e`) se hizo sin tocar dominio.

**Negativas:**

- 5 proyectos en una solution añaden tiempo de build (~30s vs ~5s del monolito).
- Cierta indirección que, en un proyecto sin equipo, es overhead. (Observado en commits "More changes" de la prehistoria — claramente la separación se introdujo cuando empezó a doler).
- El `Features/` namespace es ambiguo: contiene "casos de uso" pero no toda la capa de aplicación; `App/Aplication/` (sic, con typo legacy) sigue conteniendo `UseCases/` mezclados (LoadTableMap, SaveTableMap, EncrypterHelper). Inconsistencia.

**Implicaciones para una migración:**

- En cualquier paradigma alternativo hay que preservar el límite `Domain` (sin externals) y la separación motor/UI.
- La carpeta `App/Aplication/UseCases/` debería migrar a `Features/` para coherencia. Es deuda técnica residual.

## Referencias

- `OpenScrape.sln` — define los 5 proyectos.
- CLAUDE.md sección "Architecture" — describe los 5 layers.
- Commit `340d664 feat(decision-maker): Add new project and enhance logic` — primera separación.
- `_reversa_sdd/code-analysis.md` — análisis de cada proyecto.
