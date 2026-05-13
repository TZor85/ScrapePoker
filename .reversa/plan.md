# Plano de Exploração — ScrapePoker

> Criado pelo Reversa em 2026-05-04
> Marque cada tarefa com ✅ quando concluída.
> Você pode editar este plano antes de iniciar: adicione, remova ou reordene tarefas conforme necessário.

---

## Fase 1: Reconhecimento 🔍

- [x] ✅ **Scout** — Mapeamento de estrutura de pastas e tecnologias
- [x] ✅ **Scout** — Análise de dependências e gerenciadores de pacotes
- [x] ✅ **Scout** — Identificação de entry points, CI/CD e configurações

## Decisão de organização das specs 🗂️

> Entre o Scout e o Arqueólogo, o Reversa pergunta como você quer organizar as specs (por módulo, caso de uso, endpoint, híbrida, por features ou customizada). A escolha fica persistida em `.reversa/config.toml` na seção `[specs]` e não será reperguntada em execuções futuras. Para reapresentar o menu, remova manualmente a seção.

## Fase 2: Escavação 🏗️

> Módulos reales identificados por el Scout (`.reversa/context/surface.json` → `modules`).
> Ordenados por flujo de dependencias (Domain primero, App al final).

- [x] ✅ **Arqueólogo** — Análisis del módulo `OpenScrape.Domain` (entidades, value objects, enums, mappers, exceptions)
- [x] ✅ **Arqueólogo** — Análisis del módulo `OpenScrape.Infrastructure` (Marten setup, document store, índices)
- [x] ✅ **Arqueólogo** — Análisis del módulo `OpenScrape.Features` (use cases scoped: Table, Cards, ActionScenario, GameRound, RegionsTableMap)
- [x] ✅ **Arqueólogo** — Análisis del módulo `OpenScrape.DecisionMaker` (algorithms, services, motor de decisión completo)
- [x] ✅ **Arqueólogo** — Análisis del módulo `OpenScrape.App` (composition root, WinForms, OCR, game loop, telemetría, helpers)

## Fase 3: Interpretação 🧠

- [x] ✅ **Detetive** — Arqueologia Git e ADRs retroativos (20 ADRs)
- [x] ✅ **Detetive** — Regras de negócio implícitas e máquinas de estado (64 regras, 7 FSMs)
- [x] ✅ **Detetive** — Matriz de permissões (RBAC/ACL) (sin RBAC actual + spec licencias propuesta)
- [x] ✅ **Arquiteto** — Diagramas C4 (Contexto, Containers, Componentes)
- [x] ✅ **Arquiteto** — ERD completo e integrações externas
- [x] ✅ **Arquiteto** — Spec Impact Matrix

## Fase 4: Geração 📝

- [x] ✅ **Redator** — Specs SDD por componente (5 units × 7 archivos = 35 archivos)
- [x] ✅ **Redator** — OpenAPI (N/A — desktop WinForms sin contrato HTTP/RPC)
- [x] ✅ **Redator** — User Stories (7 historias + README índice)
- [x] ✅ **Redator** — Code/Spec Matrix (traceability/code-spec-matrix.md, cobertura 96 %)

## Fase 5: Revisão ✅

- [x] ✅ **Revisor** — Revisão cruzada de specs
- [x] ✅ **Revisor** — Resolução de lacunas com o usuário
- [x] ✅ **Revisor** — Relatório de confiança final

---

## Agentes Independentes

> Execute estes agentes quando os recursos estiverem disponíveis — podem rodar em qualquer fase.

- [ ] **Visor** — Análise de interface via screenshots
- [ ] **Data Master** — Análise completa do banco de dados
- [ ] **Design System** — Extração de tokens de design
- [ ] **Tracer** — Análise dinâmica (requer sistema acessível)

---

## Próximo passo

Após o Time de Descoberta concluir e o `_reversa_sdd/` estar populado, você pode disparar um dos fluxos seguintes:

- `/reversa-migrate`: orquestrador do **Time de Migração** (Paradigm Advisor → Curator → Strategist → Designer → Inspector). Gera as specs do sistema novo. Saída em `_reversa_sdd/migration/`.
- `/reversa-reconstructor`: gera plano bottom-up para reimplementar o software a partir das specs do legado (uma tarefa por sessão).
