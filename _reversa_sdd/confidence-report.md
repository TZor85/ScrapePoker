# Relatorio de Confianza - ScrapePoker

> Generado por el Revisor en 2026-05-12.
> Alcance: units SDD en `_reversa_sdd/OpenScrape.*` y matrices globales.

---

## Resumen General

| Nivel | Cantidad | Percentual |
|-------|----------|------------|
| 🟢 CONFIRMADO | 1118 | 56.9 % |
| 🟡 INFERIDO | 532 | 27.1 % |
| 🔴 LACUNA | 314 | 16.0 % |
| **Total** | 1964 | 100 % |

**Confianza general:** 70.5 %.

Calculo: `(1118 + 532 * 0.5) / 1964`.

---

## Por Unit

| Unit | 🟢 | 🟡 | 🔴 | Confianza |
|------|----|----|----|-----------|
| `OpenScrape.Domain` | 167 | 66 | 23 | 78.1 % |
| `OpenScrape.App` | 366 | 149 | 85 | 73.4 % |
| `OpenScrape.DecisionMaker` | 293 | 147 | 88 | 69.4 % |
| `OpenScrape.Features` | 173 | 122 | 70 | 64.1 % |
| `OpenScrape.Infrastructure` | 112 | 48 | 55 | 63.3 % |

---

## Revision Cruzada

- Engine externa consultada: no disponible en esta sesion (`codex:*` no expuesto).
- Apuntamientos recibidos: 0.
- Aceptados: 0 | Rechazados: 0 | Pendientes: 0.

---

## Validacion De Matrices

- `traceability/code-spec-matrix.md`: consistente con `surface.json`. Cinco units esperadas y cinco units generadas. Canónicos `requirements.md`, `design.md`, `tasks.md` presentes en todas las units.
- Cobertura declarada: 228 archivos C# productivos; 218 cubiertos, 5 parciales, 0 sin spec, 5 n/a. Sin modulo productivo omitido frente a `surface.json`.
- `traceability/spec-impact-matrix.md`: consistente con arquitectura y dependencias principales. Mantiene como impactos abiertos las deudas detectadas en units: `FrmMain`, `PostflopDecisionService`, credenciales, `BigBlind=1.0`, tablemaps configurables y `EncrypterHelper`.

---

## Lacunas Pendientes 🔴

### `OpenScrape.Infrastructure`

- `Program.cs:52` pasa `true` a `AddDataBase`; `Services.cs:36-38` activa `AutoCreate.All`. Decision humana cerrada: cambiar a entorno real (`context.HostingEnvironment.IsDevelopment()` o equivalente).
- `Services.cs:16` usa `GetConnectionString("DefaultConnection")!`; falta politica de error explicito ante config ausente.
- No hay estrategia de migracion productiva versionada para Marten/PostgreSQL.

### `OpenScrape.DecisionMaker`

- `AutoCalibrationService.cs:177,185,193,201` usa `OldValue` hardcoded. Es bug confirmado, pero falta decision de compatibilidad UX.
- `ExploitabilityCalculator.cs:93` fija `BigBlind = 1.0` aunque dominio/sesion tienen `BigBlind`. Decision humana cerrada: usar BigBlind desde sesion actual.
- `OpponentTracker` vive en memoria; decision humana cerrada: asignar GUID por alias OCR y persistir por ese id.
- `PostflopDecisionService` no valida explicitamente rangos criticos de input antes de decidir.

### `OpenScrape.Features`

- `GetActionScenario.cs:31` tiene filtro `BetSize` comentado mientras JSON contiene `BetSize`. Decision humana cerrada: devolver `BetSize` al lookup.
- `GetAllTables`, `GetAllRegionTableMap` y `GetFlopCards` estan registrados/declarados como placeholders. Decision humana cerrada: eliminarlos.
- `UpdateRegionTableMap` conserva flags previos e ignora flags nuevos en creacion inicial; falta politica.

### `OpenScrape.App`

- `appsettings.json` contiene credenciales reales. Decision humana cerrada para reconstruccion: mantener compatibilidad exacta actual.
- `EncrypterHelper.cs:122,134` usa IV fija. Decision humana cerrada para reconstruccion: mantener compatibilidad exacta actual.
- Paths absolutos en `FormImage.cs` y use cases de tablemap. Decision humana cerrada: carpeta configurable por usuario.
- `eng.traineddata` duplicado en csproj. Falta decidir recurso unico.
- `FormListApps.cs:32` filtra `"NL H"`. Falta decidir si se parametriza.

### `OpenScrape.Domain`

- `OpponentProfile` no persistido cross-sesion; usuario confirmo que deberia persistirse, pero falta contrato y propietario.
- `AutoRebuy`: usuario indico formula `100 BB - currentStack`; falta reflejarla como regla de dominio y cubrir caso top-up manual.
- `StreetDecision`/historial: usuario confirmo que historial no debe editar manos; falta convertirlo en requisito explicito.

---

## Reclassificaciones

| De | Para | Afirmacion | Evidencia |
|----|------|------------|-----------|
| 🔴 | 🟢 | Uso personal/educativo, sin modelo comercial | Respuesta Q-DOM-01 |
| 🔴 | 🟢 | Salas soportadas dependen del `tablemap` del usuario | Respuesta Q-DOM-02 |
| 🔴 | 🟢 | Calibracion actual para micro-stakes NL2-NL10 | Respuesta Q-DOM-03 |
| 🔴 | 🟢 | Subir/bajar stake lo decide el usuario | Respuesta Q-DOM-04 |
| 🔴 | 🟢 | Historial no permite editar manos | Respuesta Q-DOM-05 |
| 🔴 | 🟢 | Licencias quedan diferidas | Respuesta Q-DOM-08 |
| 🔴 | 🟡 | AutoIt/automatizacion esta dormido para futura implementacion | Respuesta Q-DOM-06 |
| 🔴 | 🟡 | Coaching necesita vista de divergencia bot/humano | Respuesta Q-DOM-07 |
| 🔴 | 🟡 | `OpponentProfile` deberia persistirse en futuro | Respuesta Q-FSM-02 |
| 🔴 | 🟡 | Credenciales reales fueron decision consciente, pero requieren rotacion futura | Respuestas Q-DOM-10 y Q-PERM-CRED-01 |
| 🔴 | 🟡 | Licencia no deberia permitir uso offline directo | Respuesta Q-FSM-LIC-01 |
| 🔴 | 🟡 | Alias de oponentes deberian anonimizarse como `player1`, `player2`, ... | Respuesta Q-PERM-DATA-02 |
| 🔴 | 🟢 | `IsDevelopment=true` debe cambiar a deteccion real de entorno | Respuesta Q-REV-01 |
| 🔴 | 🟢 | Reconstruccion mantiene compatibilidad exacta con config/cifrado actual | Respuesta Q-REV-02 |
| 🔴 | 🟢 | `OpponentProfile` se persiste con GUID asignado por alias | Respuesta Q-REV-03 |
| 🔴 | 🟢 | `BetSize` vuelve al lookup preflop | Respuesta Q-REV-04 |
| 🔴 | 🟢 | Use cases placeholder se eliminan | Respuesta Q-REV-05 |
| 🔴 | 🟢 | `ExploitabilityCalculator` toma BigBlind desde sesion actual | Respuesta Q-REV-06 |
| 🔴 | 🟢 | Recursos/tablemaps usan carpeta configurable por usuario | Respuesta Q-REV-07 |

---

## Recomendaciones

- Priorizar seguridad/config: DPAPI, disclaimer/TOS y validacion explicita de connection string.
- Implementar decisiones cerradas: entorno real para Marten, GUID por alias, `BetSize` en lookup, eliminar placeholders, BigBlind desde sesion y carpeta configurable.
- Resolver hardening de DecisionMaker antes de reconstruccion: `OldValue`, input guards y `Random.Shared`.
