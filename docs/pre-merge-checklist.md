# Pre-Merge Checklist

Ejecutar estos checkpoints antes de merge a develop.

## Checkpoints

### 1. Build Debug
```powershell
dotnet build OpenScrape.sln
```
**Esperado:** Exit code 0

### 2. Build Release
```powershell
dotnet build OpenScrape.sln --configuration Release
```
**Esperado:** Exit code 0

### 3. Format
```powershell
dotnet format --verify-no-changes OpenScrape.sln
```
**Esperado:** Sin cambios detectados

### 4. Tests
```powershell
dotnet test OpenScrape.sln
```
**Esperado:** ≥ 1173 tests pasando

### 5. Warnings
```powershell
dotnet build OpenScrape.sln 2>&1 | Select-String "warning" | Measure-Object
```
**Esperado:** ≤ 75 warnings

### 6. csproj Changes
```powershell
git diff main --stat -- "**/*.csproj"
```
**Esperado:** 0 archivos modificados

### 7. Smoke Test (Manual)

| # | Paso |
|---|------|
| 1 | Abrir aplicación |
| 2 | Conectar OCR |
| 3 | Seleccionar mesa |
| 4 | Capturar mano |
| 5 | Tomar decisión |
| 6 | Verificar UI actualiza |
| 7 | Repetir ciclo 3 veces |
| 8 | Cerrar aplicación sin errores |

**Esperado:** Todos OK

## Ejecución Automatizada

```powershell
# Con script (skip smoke test)
.\scripts\verify-pre-merge.ps1 -SkipSmoke

# Con verbose
.\scripts\verify-pre-merge.ps1 -Verbose
```

## Notas

- Ejecutar antes de: merge a develop, PR, release build
- Si csproj changed, revisar si las dependencias son necesarias
- Smoke test es manual por naturaleza visual/OCR