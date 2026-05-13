# Gaps - ScrapePoker

> Generado por el Revisor en 2026-05-12.
> `doc_level=detalhado`: lacunas categorizadas por severidad.

---

## Critico

- **Credenciales reales en config versionada.** Riesgo activo si repo/binario se comparte. Usuario confirmo que deben rotarse y moverse, aunque la decision original fue consciente.
- **`EncrypterHelper` con IV fija.** Decision de reconstruccion: mantener compatibilidad exacta; deuda de seguridad queda documentada.
- **`IsDevelopment=true` hardcoded.** Decision cerrada: cambiar a deteccion real de entorno.
- **DecisionMaker sin guardias de input.** Equity NaN/fuera de rango, pot invalido o stack negativo pueden propagar decisiones absurdas.
- **`AutoCalibrationService.OldValue` hardcoded.** Bug confirmado: recomendaciones mostradas al usuario pueden partir de valores falsos.

## Moderado

- **Persistencia de `OpponentProfile`.** Decision cerrada: asignar GUID a cada alias y persistir por ese id.
- **`ExploitabilityCalculator.BigBlind=1.0`.** Decision cerrada: tomar BB de sesion actual.
- **`BetSize` comentado en `GetActionScenario`.** Decision cerrada: volver al lookup.
- **Use cases placeholder.** Decision cerrada: eliminar `GetAllTables`, `GetAllRegionTableMap`, `GetFlopCards`.
- **Paths absolutos de recursos.** Decision cerrada: carpeta configurable por usuario.
- **Filtro `"NL H"` hardcoded.** Limita deteccion de ventanas a naming actual.
- **Tesseract `eng.traineddata` duplicado.** Recurso embebido doble; requiere decision de packaging.

## Cosmetico / Mantenibilidad

- **Legacy `HandEvaluator` junto a `BitHandEvaluator`.** Cobertura parcial y decision de descarte pendiente.
- **`FrmMain` god-class.** Riesgo alto de mantenimiento, pero specs ya documentan refactor por fases.
- **`PostflopDecisionService` god-service.** Riesgo alto de mantenimiento, pero no bloquea reproduccion 1:1.
- **Carpetas/proyectos vestigio.** `OpenScrape.Application`, `OpenScrape.Core`, `MainPage.xaml*` aparecen como restos de refactor.

---

## Preguntas Pendientes

Sin preguntas pendientes del Revisor. Quedan gaps tecnicos para implementacion.
