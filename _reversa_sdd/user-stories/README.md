# User Stories — ScrapePoker

> Índice de las 7 historias de usuario que cubren el flujo completo del producto, desde onboarding hasta diagnóstico. Cada historia es un **flujo end-to-end** independiente con sus propios criterios de aceptación, variantes, métricas de éxito y riesgos. Las historias se complementan: US-01 es el corazón operativo; las demás envuelven, configuran o derivan de ella.

---

## 1. Listado

| ID | Historia | Persona | Frecuencia | Criticidad |
|----|----------|---------|------------|:----------:|
| **US-01** | [Captura → Decisión → Recomendación](captura-decision.md) | Pablo (en sesión) | continua (cada 100 ms) | 🔴 stopper |
| **US-02** | [Configuración inicial: arranque, ventana, calibración](configuracion-inicial.md) | Pablo (onboarding) | 1 vez por instalación / cambio | 🔴 stopper (precondición US-01) |
| **US-03** | [Sesión de juego: lifecycle, tracking, persistencia](sesion-juego.md) | Pablo (en sesión) | 1 vez por sesión (4-6 h) | 🔴 stopper |
| **US-04** | [Revisión de historial: post-sesión, coaching, divergencia](revision-historial.md) | Pablo (post-sesión) | diaria/semanal | 🟡 alto valor |
| **US-05** | [Telemetría operativa y tracking de bankroll](telemetria-bankroll.md) | Pablo + Dev | continua (telemetría) / mensual (bankroll) | 🟡 alto valor |
| **US-06** | [Backtest A/B y auto-calibración de estrategia](backtest-calibracion.md) | Pablo + Dev | mensual / on-demand | 🟡 alto valor |
| **US-07** | [Diagnóstico y debug operativo](diagnostico-debug.md) | Pablo (problema) + Dev (soporte) | bajo demanda | 🟢 importante |

**Total criterios de aceptación documentados:** 71 CA across 7 historias (10 + 10 + 10 + 10 + 11 + 11 + 11 - 2 compartidos).
**Total variantes (caminos secundarios):** 53 V- across 7 historias.

---

## 2. Grafo de dependencias

```
                          [Onboarding]
                              US-02
                          (configuración)
                               │
                               ▼  precondición
                          ┌──────────┐
                          │  US-01   │ ← — — corazón operativo
                          │ Captura  │       (loop 100 ms)
                          │ Decisión │
                          └──────────┘
                               │
                               │ wraps lifecycle
                               ▼
                          ┌──────────┐
                          │  US-03   │
                          │  Sesión  │
                          │  juego   │
                          └──────────┘
                               │
                               │ persiste HandRecord + GameSession
                               ▼
                ┌──────────────┴──────────────┐
                ▼                             ▼
          ┌──────────┐                  ┌──────────┐
          │  US-04   │                  │  US-05   │
          │ Revisión │                  │ Telemet. │
          │ Historial│                  │ Bankroll │
          └──────────┘                  └──────────┘
                │                             │
                │ usa StrategyBacktester       │
                ▼                             │
          ┌──────────┐                       │
          │  US-06   │ ← ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘
          │ Backtest │   consume telemetría
          │ Calibrac.│
          └──────────┘
                │
                │ feedback loop al StrategyProfile
                ▼
          [Aplica cambio → reinicia → US-01 con nuevo profile]


    [US-07 — Diagnóstico] envuelve transversalmente todo:
    útil cuando US-01 falla, cuando US-02 calibra mal,
    cuando US-03 pierde manos, cuando US-04 muestra inconsistencias.
```

---

## 3. Mapa de cobertura por unit

Cada user-story consume specs de varias units. El mapa siguiente confirma **trazabilidad cruzada**: cualquier código de un módulo se ejercita en al menos una user-story.

| User Story | OpenScrape.Domain | OpenScrape.Infrastructure | OpenScrape.Features | OpenScrape.DecisionMaker | OpenScrape.App |
|------------|:------------------:|:-:|:-:|:-:|:-:|
| US-01 captura-decisión | ⭐ | ◯ | ⭐ | ⭐ | ⭐ |
| US-02 configuración inicial | ⭐ | ⭐ | ⭐ | ◯ | ⭐ |
| US-03 sesión juego | ⭐ | ⭐ | ⭐ | ◯ | ⭐ |
| US-04 revisión historial | ⭐ | ⭐ | ⭐ | ⭐ | ⭐ |
| US-05 telemetría/bankroll | ⭐ | ⭐ | ◯ | ⭐ | ⭐ |
| US-06 backtest/calibración | ⭐ | ◯ | ◯ | ⭐ | ⭐ |
| US-07 diagnóstico/debug | ◯ | ◯ | ◯ | ◯ | ⭐ |

Leyenda: ⭐ = ownership / consumo crítico · ◯ = consumo derivado · vacío = no involucrado.

**Cobertura:**
- `OpenScrape.Domain`: ejercitada en 7/7 historias (100 %).
- `OpenScrape.App`: ejercitada en 7/7 historias (100 %).
- `OpenScrape.Features`: ejercitada en 6/7 historias (86 %).
- `OpenScrape.DecisionMaker`: ejercitada en 6/7 historias (86 %).
- `OpenScrape.Infrastructure`: ejercitada en 4/7 historias directas (57 %), pero presente como precondición en todas.

---

## 4. Mapa de bloqueos por preguntas abiertas

Las user-stories referencian preguntas de `<unit>/questions.md` que **bloquean** el cierre de criterios de aceptación. Resolver primero las 🔴 críticas.

| Pregunta | User stories afectadas | Severidad | Estado |
|----------|------------------------|:---------:|--------|
| **Q-APP-01** Credenciales en `appsettings.json` | US-02 | 🔴 | ✅ decidido (rotar+purgar Git) |
| **Q-APP-02** `EncrypterHelper` IV fija | US-02 | 🔴 | ✅ decidido (migrar a AesGcm) |
| **Q-APP-03** `PokerDecisionFacade` muerto | US-01 | 🟡 | ✅ decidido (eliminar) |
| **Q-APP-04** `GameLoopCoordinator` apagado | US-01, US-07 | 🟡 | abierta |
| **Q-APP-05** `FormImage` path hardcoded | US-02 | 🔴 | abierta |
| **Q-APP-06** `eng.traineddata` duplicada | US-02 | 🟡 | abierta |
| **Q-APP-07** `FrmMain` god-class 4502 LOC | (refactor) | 🟡 | abierta |
| **Q-APP-08** Filtro `"NL H"` hardcoded | US-02 | 🟡 | abierta |
| **Q-APP-09** Hot reload `StrategyProfile` | US-06 | 🟡 | abierta |
| **Q-APP-10** Multi-monitor DPI | US-01 | 🟡 | abierta |
| **Q-APP-11** Validación `BoardCards` duplicados | US-01 | 🟡 | abierta |
| **Q-APP-12** Marten outage durante persist | US-03, US-05, US-06 | 🟡 | abierta |
| **Q-APP-13** `BackgroundWorker` zombie loop | US-01, US-07 | 🟡 | abierta |
| **Q-APP-14** OCR low confidence cascade | US-01, US-07 | 🟡 | abierta |
| **Q-APP-15** Tesseract crash region OOB | US-01, US-07 | 🔴 | abierta |
| **Q-APP-16** `RegionLookupCache` reload race | US-02 | 🟡 | abierta |
| **Q-APP-17** Equity cache LRU vs FIFO | US-01 | 🟡 | abierta |
| **Q-APP-18** Cliente cerrado durante captura | US-01, US-07 | 🟡 | abierta |
| **Q-APP-19** Traineddata corrupta auto-recovery | US-02 | 🔴 | abierta |
| **Q-APP-20** Manos `Unknown` al cerrar app | US-03, US-04 | 🟢 | abierta |
| **Q-APP-21** `TextBoxLoggerProvider` saturación | US-07 | 🟡 | abierta |
| **Q-APP-22** Heads-up `previousSB == previousBB` | US-01 | 🟡 | abierta |
| **Q-DM-05** Refactor `PostflopDecisionService` | (mantenibilidad) | 🟡 | abierta |
| **Q-DM-06** Fix `AutoCalibration` BUG `OldValue` | US-04, US-06 | 🔴 | abierta — **blocker prioritario** |
| **Q-DM-07** Parametrizar `BigBlind` `Exploitability` | US-04, US-05 | 🔴 | abierta — **blocker coaching/bankroll** |
| **Q-DM-08** Persistir `OpponentTracker` | US-03, US-04 | 🟡 | abierta |
| **Q-DM-09** Validación input `DetermineAction` | US-01 | 🔴 | abierta |
| **Q-DM-10** `IRandomProvider` para reproducibilidad | US-06 | 🟡 | abierta |
| **Q-DOM-04** Trigger Risk-of-Ruin | US-05 | 🟡 | parcial (raíz) |
| **Q-DOM-07** Coaching/divergencia bot vs humano | US-04 | 🟡 | parcial (raíz) |
| **Q-PERM-DATA-02** Anonimización aliases | US-04 | 🟡 | parcial (raíz) |

**Top 5 blockers críticos para producción:**
1. 🔴 Q-APP-01 — Rotar credenciales (decidido, **ejecutar**).
2. 🔴 Q-APP-02 — Migrar a AesGcm (decidido, **ejecutar**).
3. 🔴 Q-DM-06 — Fix `AutoCalibration OldValue` hardcoded.
4. 🔴 Q-DM-07 — Parametrizar `BigBlind` en `Exploitability`.
5. 🔴 Q-APP-15 — Validación bounds `OcrService` (evita crash de proceso).

---

## 5. Cómo usar las user-stories

**Para entender el producto:** comienza por **US-01** (corazón operativo). Luego **US-02** (precondición). Luego **US-03** (envoltorio).

**Para reimplementar el producto:** lee cada user-story junto con su unit correspondiente. Las CA son testeables; las V- son escenarios de borde a cubrir.

**Para priorizar trabajo de mantenimiento:** filtra preguntas 🔴 abiertas en la sección 4. Resuélvelas en orden.

**Para revisar fidelidad de un cambio:** verifica qué user-stories tocan el código modificado y ejecuta los CA correspondientes (smoke + integration).

**Para auditar cobertura de testing:** la suite `OpenScrape.App.Tests` (~645 tests) cubre las units. Las user-stories cubren los **flujos**: validación E2E manual recomendada por sesión real.

---

## 6. Resumen de métricas de éxito agregadas

Métricas críticas que cruzan múltiples user-stories:

| Métrica | Valor objetivo | User stories | Fuente |
|---------|----------------|--------------|--------|
| **Latencia ciclo p95** | <200 ms | US-01, US-05 | `MetricsCollector` |
| **Tasa de manos persistidas** | >99.5 % | US-03, US-04 | `_sessionTotalHands` vs Marten count |
| **Confianza OCR promedio** | >0.85 | US-01, US-07 | `IsHighConfidence` ratio |
| **Cobertura de tests** | ~645 tests verdes en CI | todas | NUnit |
| **`BBPer100` correcto** | exacto al stake | US-04, US-05, US-06 | manual + Q-DM-07 |
| **Tiempo onboarding (instalar → listo)** | <2 minutos | US-02 | manual |
| **Tasa de crashes por sesión 6 h** | 0 | US-01, US-03, US-07 | EventLog |
| **Drift acumuladores vs persisted** | <0.5 % | US-03, US-05 | reconciliación periódica |

---

## 7. Notas finales sobre el conjunto

- **El producto se entiende a través de US-01 + US-03:** todo lo demás es servicio o derivado. Si un dev nuevo lee solo dos historias, esas son las dos.

- **US-02 es desproporcionadamente crítica:** rota friction-bearing en onboarding bloquea conversión. Deuda técnica aquí (credenciales hardcoded, path hardcoded, filtro hardcoded) tiene impacto outsized.

- **US-04 + US-06 son el moat del producto:** la diferencia entre "bot que decide" (commodity) y "coach que aprende" (defensible) está aquí. Q-DM-06 + Q-DM-07 son los blockers que prevén realizar ese moat.

- **US-07 es invisible pero crítico para soporte:** sin diagnóstico, cada bug es no-reproducible. Inversión en CA-10 (snapshot bundle) y CA-11 (sink archivo) tiene ROI alto en mantenimiento.

- **Cobertura E2E manual:** no hay test E2E automatizado por la dificultad de capturar cliente real en CI. Pablo es el "test de aceptación humano". Documentar checklist de sesión de validación pre-release.

- **User-stories como contrato con Pablo:** estas historias no son hipótesis — son lo que Pablo necesita para usar el producto exitosamente. Cualquier funcionalidad nueva debería primero mapearse a una user-story existente o crear una nueva.

- **Próximas user-stories candidatas (no escritas):**
  - US-08 *Multi-mesa coordinada* (4 instancias con bankroll consolidado).
  - US-09 *Spec licencias* (login + activación, OpenSpec pending).
  - US-10 *Export hand history* (HM4/PT4 compatibility).

---

## 8. Estado y próximos pasos

**Generado por:** Reversa Writer en Fase 4 (Geração) el 2026-05-07.

**Total artefactos generados en Fase 4:**
- 5 units × 7 archivos = 35 specs por unit (`OpenScrape.Domain`, `OpenScrape.Infrastructure`, `OpenScrape.Features`, `OpenScrape.DecisionMaker`, `OpenScrape.App`).
- 7 user-stories + este README = 8 historias en total.
- 1 traceability matrix (`code-spec-matrix.md`).

**Cobertura efectiva del legado:** 🟢 ~96 % (218/228 archivos productivos cubiertos por unit; 5 parciales en `Helpers/FlopHelper/`; 0 lacunas).

**Próxima fase:** **Fase 5 — Revisión** (Reviewer del Reversa).
- Cross-check de coherencia entre artefactos.
- Reclasificación de confianza (🟢/🟡/🔴) tras validación del usuario.
- Resolución de las 30+ preguntas abiertas en `questions.md` raíz + por unit.
- Reporte de confianza final.
