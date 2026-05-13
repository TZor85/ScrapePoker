## Why

Antes del merge a develop, es crítico validar que no se introdujeron regresiones de build, format, tests o dependencias. Las funcionalidades de telemetría deben funcionar correctamente y no afectar la estabilidad existente.

## What Changes

- Crear suite de verificaciones manuales documentadas
- Verificar build Debug y Release sin errores
- Verificar dotnet format limpio
- Verificar tests pasando
- Verificar warnings <= 75
- Verificar sin cambios en .csproj (sin deps nuevas)
- Ejecutar smoke test de 8 pasos

## Capabilities

### New Capabilities

- `telemetry-quality`: Validación de calidad pre-merge para features de telemetría

## Impact

- Archivos afectados: Documentación de verificación, scripts optional

## Acceptance Criteria

- Build Debug+Release sin errores
- dotnet format --verify-no-changes limpio
- 1173 tests pasando
- <= 75 warnings
- git diff main --stat -- '**/*.csproj' sin cambios
- Smoke test 8 pasos OK