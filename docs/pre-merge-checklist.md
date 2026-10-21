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
**Esperado:** Exit code 0 (≥ 1300 tests pasando)

### 5. Warnings
```powershell
dotnet build OpenScrape.sln --configuration Release --no-incremental `
  -p:TreatWarningsAsErrors=true `
  -warnaserror:"NU1901;NU1902;NU1903;NU1904"
```
**Esperado:** Exit code 0 (0 advertencias)

No contar líneas de texto que digan "warning": la salida del CLI está localizada
y el patrón no matchea en un sistema en español. Y `--no-incremental` es
obligatorio, porque un build incremental no recompila y no re-emite las
advertencias, con lo que el checkpoint pasaría sin verificar nada.

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