# verify-pre-merge.ps1
param(
    [switch]$SkipSmoke,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$results = @()
$startTime = Get-Date

function Test-Checkpoint {
    param($Name, $ScriptBlock)
    try {
        if ($Verbose) {
            Write-Host "Running: $Name" -ForegroundColor Cyan
        }
        & $ScriptBlock
        $script:results += @{ Name = $Name; Status = "PASS" }
        if ($Verbose) {
            Write-Host "  PASS" -ForegroundColor Green
        }
    } catch {
        $script:results += @{ Name = $Name; Status = "FAIL"; Error = $_.Exception.Message }
        if ($Verbose) {
            Write-Host "  FAIL: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host "=== Pre-Merge Checkpoints ===" -ForegroundColor Yellow

# Checkpoint 1: Build Debug
Test-Checkpoint "Build Debug" {
    $output = dotnet build OpenScrape.sln 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}

# Checkpoint 2: Build Release
Test-Checkpoint "Build Release" {
    $output = dotnet build OpenScrape.sln --configuration Release 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "Build Release failed" }
}

# Checkpoint 3: Format
Test-Checkpoint "Format" {
    dotnet format OpenScrape.sln --verify-no-changes 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Format detected changes" }
}

# Checkpoint 4: Tests
# Se usa el exit code y no el texto: la salida del CLI esta localizada y un
# patron en ingles ("Passed!") no matchea en un sistema en espanol.
Test-Checkpoint "Tests" {
    dotnet test OpenScrape.sln --no-build --verbosity quiet 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Tests failed (exit $LASTEXITCODE)" }
}

# Checkpoint 5: Warnings
# Cero advertencias, mismo criterio que el CI. --no-incremental es obligatorio:
# un build incremental no recompila y no re-emite las advertencias, con lo que
# el checkpoint pasaria sin verificar nada.
Test-Checkpoint "Warnings" {
    $flags = @(
        '-p:TreatWarningsAsErrors=true',
        '-warnaserror:NU1901;NU1902;NU1903;NU1904'
    )
    dotnet build OpenScrape.sln --configuration Release --no-incremental @flags 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "El build reporta advertencias del compilador o paquetes vulnerables"
    }
    if ($Verbose) {
        Write-Host "  Warnings: 0" -ForegroundColor Green
    }
}

# Checkpoint 6: csproj changes
# El baseline es el merge-base con develop, que es la rama destino del pre-merge.
# Comparar contra main daba falsos positivos: main es una rama vieja que ni
# siquiera contiene todos los proyectos. Se cuentan las rutas devueltas por
# --name-only y no el texto del resumen: la salida de git esta localizada y un
# patron en ingles ("files changed") no matchea en un sistema en espanol.
Test-Checkpoint "No csproj Changes" {
    $developRef = $null
    git show-ref --verify --quiet refs/heads/develop
    if ($LASTEXITCODE -eq 0) { $developRef = "develop" }
    if (-not $developRef) {
        git show-ref --verify --quiet refs/remotes/origin/develop
        if ($LASTEXITCODE -eq 0) { $developRef = "origin/develop" }
    }
    if (-not $developRef) {
        throw "No se encontro la rama develop para calcular el baseline"
    }

    $mergeBase = (git merge-base HEAD $developRef 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $mergeBase -notmatch '^[0-9a-f]{7,40}$') {
        throw "No se pudo resolver el merge-base con $developRef"
    }

    $changed = @(git diff --name-only $mergeBase -- "*.csproj")
    if ($changed.Count -gt 0) {
        throw "csproj modificados respecto de $developRef ($($changed -join ', '))"
    }
}

# Checkpoint 7: Smoke test (optional)
if (-not $SkipSmoke) {
    Test-Checkpoint "Smoke Test" {
        Write-Host ""
        Write-Host "=== Smoke Test (Manual) ===" -ForegroundColor Yellow
        Write-Host "1. Abrir aplicación"
        Write-Host "2. Conectar OCR"
        Write-Host "3. Seleccionar mesa"
        Write-Host "4. Capturar mano"
        Write-Host "5. Tomar decisión"
        Write-Host "6. Verificar UI actualiza"
        Write-Host "7. Repetir ciclo 3 veces"
        Write-Host "8. Cerrar aplicación sin errores"
        Write-Host ""
        Write-Host "Presiona Enter cuando completes el smoke test..." -ForegroundColor Cyan
        Read-Host
    }
}

# Summary
$elapsed = (Get-Date) - $startTime
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Yellow
$results | ForEach-Object {
    $color = if ($_.Status -eq "PASS") { "Green" } else { "Red" }
    Write-Host ("  [{0}] {1}" -f $_.Status, $_.Name) -ForegroundColor $color
}
Write-Host ""
$failed = @($results | Where-Object { $_.Status -eq "FAIL" })
if ($failed.Count -gt 0) {
    Write-Host "FAILED: $($failed.Count) checkpoint(s)" -ForegroundColor Red
    Write-Host "Elapsed: $($elapsed.TotalSeconds)s" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "ALL CHECKPOINTS PASSED" -ForegroundColor Green
    Write-Host "Elapsed: $($elapsed.TotalSeconds)s" -ForegroundColor Yellow
    exit 0
}