# Proposal: Checkpoints Finales de Telemetría

## Capability
`telemetry-quality` (new)

## Why
Antes del merge a develop, necesitamos una suite de verificaciones documentadas para detectar regresiones de build, format, tests y dependencias antes de que se introduzcan al codebase principal.

## What Changes
Script/checklist de verificación pre-merge con los 6 checkpoints actuales del proyecto.

## Impact
- **Archivo nuevo**: Script de verificación o checklist en `docs/`
- **Dependencia**: Ninguna nueva, usa comandos existentes del proyecto

## Capabilities involved
- `dotnet build`
- `dotnet format`
- `dotnet test`
- `git diff`

## Acceptance Criteria

### AC1: Build Debug sin errores
- **Cuando** se ejecuta `dotnet build OpenScrape.sln`
- **Entonces** Exit code 0

### AC2: Build Release sin errores
- **Cuando** se ejecuta `dotnet build OpenScrape.sln --configuration Release`
- **Entonces** Exit code 0

### AC3: Format limpio
- **Cuando** se ejecuta `dotnet format --verify-no-changes OpenScrape.sln`
- **Entonces** Sin cambios detectados

### AC4: Tests pasando
- **Cuando** se ejecuta `dotnet test OpenScrape.sln`
- **Entonces** ≥ 1173 tests pasando

### AC5: Warnings ≤ 75
- **Cuando** se compila con warn-as-error disabled
- **Entonces** ≤ 75 warnings

### AC6: Sin cambios en .csproj
- **Cuando** se ejecuta `git diff main --stat -- '**/*.csproj'`
- **Entonces** Sin archivos modificados (0 changes)

### AC7: Smoke test manual
- **Cuando** se ejecutan los 8 pasos de smoke test
- **Entonces** Todos OK