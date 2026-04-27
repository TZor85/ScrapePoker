# Tasks: Checkpoints Finales de Telemetría

## Task 1: Crear script de verificación

- [x] ~~Crear directorio scripts si no existe~~
- [x] ~~Crear script verify-pre-merge.ps1~~

## Task 2: Testing local del script

- [x] ~~Ejecutar build debug~~ - PASS (0 errors, 77 warnings)
- [x] ~~Ejecutar build release~~ - PASS (0 errors, 77 warnings)
- [x] ~~Verificar format~~ - PASS (sin cambios)
- [x] ~~Verificar tests~~ - PASS (1176 tests passing)
- [x] ~~Verificar warnings~~ - FAIL (77 > 75, sobre el límite)
- [x] ~~Verificar csproj diff~~ - Falla en rama feature (esperado)

## Task 3: Documentar checklist alternativo

- [x] ~~Crear docs/pre-merge-checklist.md~~

## Task 4: Integrar con workflow existente

- [x] ~~Agregar instrucciones al AGENTS.md~~

## Notas

- Los 77 warnings exceden el límite de 75. Para adjustar: `.\scripts\verify-pre-merge.ps1 -MaxWarnings 80`
- El checkpoint de csproj falla en ramas feature (esperado - no es un problema del script).
- Tests: 1176 pasando vs 1173 esperado (ok).
- Ejecutar con: `.\scripts\verify-pre-merge.ps1 -SkipSmoke` para skip smoke test manual.