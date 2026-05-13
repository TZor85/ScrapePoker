# Design: Checkpoints Finales de Telemetría

## Architecture

### Script Structure
```
scripts/
└── verify-pre-merge.ps1 (o checklist.md)
```

### Checkpoint Commands

| # | Checkpoint | Command | Expected |
|---|-----------|--------|----------|
| 1 | Build Debug | `dotnet build OpenScrape.sln` | Exit code 0 |
| 2 | Build Release | `dotnet build OpenScrape.sln --configuration Release` | Exit code 0 |
| 3 | Format | `dotnet format --verify-no-changes OpenScrape.sln` | Sin cambios |
| 4 | Tests | `dotnet test OpenScrape.sln` | ≥ 1173 passing |
| 5 | Warnings | `dotnet build OpenScrape.sln 2>&1 \| select-string warning` | ≤ 75 warnings |
| 6 | csproj diff | `git diff main --stat -- '**/*.csproj'` | 0 archivos |
| 7 | Smoke test | Manual (8 pasos) | Todos OK |

### Script Implementation

```powershell
# verify-pre-merge.ps1
param(
    [switch]$SkipSmoke,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$results = @()

function Test-Checkpoint {
    param($Name, $ScriptBlock)
    try {
        & $ScriptBlock
        $results += @{ Name = $Name; Status = "PASS" }
    } catch {
        $results += @{ Name = $Name; Status = "FAIL"; Error = $_.Exception.Message }
    }
}

# Checkpoint 1: Build Debug
Test-Checkpoint "Build Debug" {
    $output = dotnet build OpenScrape.sln 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}

# Checkpoint 2: Build Release
Test-Checkpoint "Build Release" {
    $output = dotnet build OpenScrape.sln --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Build Release failed" }
}

# Checkpoint 3: Format
Test-Checkpoint "Format" {
    dotnet format OpenScrape.sln --verify-no-changes
    if ($LASTEXITCODE -ne 0) { throw "Format detected changes" }
}

# Checkpoint 4: Tests
Test-Checkpoint "Tests" {
    $output = dotnet test OpenScrape.sln --no-build --verbosity quiet 2>&1
    # Parsear "Failed!  - Failed:     0, Passed:    1173"
    if ($output -notlike "*Passed:*1173*") { throw "Tests count mismatch" }
}

# Checkpoint 5: Warnings
Test-Checkpoint "Warnings" {
    $output = dotnet build OpenScrape.sln 2>&1 | Select-String "warning"
    $count = ($output | Measure-Object).Count
    if ($count -gt 75) { throw "Too many warnings: $count" }
}

# Checkpoint 6: csproj changes
Test-Checkpoint "No csproj Changes" {
    $output = git diff main --stat -- '**/*.csproj'
    if ($output -notmatch "0 files changed") { throw "csproj files changed" }
}

# Summary
$results | Format-Table -AutoSize
$failed = $results | Where-Object { $_.Status -eq "FAIL" }
if ($failed) {
    Write-Host "FAILED: $($failed.Count) checkpoints" -ForegroundColor Red
    exit 1
} else {
    Write-Host "ALL CHECKPOINTS PASSED" -ForegroundColor Green
    exit 0
}
```

###ubicación del script

El script debe ubicarse en `scripts/verify-pre-merge.ps1` baseado en la estructura existente del proyecto.

### smoke test Steps (8 pasos)

1. Abrir aplicación
2. Conectar OCR
3. Seleccionar mesa
4. Capturar mano
5. Tomar decisión
6. Verificar UI actualiza
7. Repetir ciclo 3 veces
8. Cerrar aplicación sin errores

## Consideraciones

### Por qué script en lugar de checklist manual
Un script es ejecutable reusable y no depende de memoria del developer.

### Por qué smoke test es manual
El smoke test requiere interacción visual con la UI y OCR, difícil de automatizar en entorno CI/CD sin cambios significativos.

### Ejecución recomendada
Ejecutar antes de:
- merge a develop
- creación de PR
- release build