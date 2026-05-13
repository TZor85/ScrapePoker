# verify-pre-merge.ps1
param(
    [switch]$SkipSmoke,
    [switch]$Verbose,
    [int]$MaxWarnings = 75
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
        $results += @{ Name = $Name; Status = "PASS" }
        if ($Verbose) {
            Write-Host "  PASS" -ForegroundColor Green
        }
    } catch {
        $results += @{ Name = $Name; Status = "FAIL"; Error = $_.Exception.Message }
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
Test-Checkpoint "Tests" {
    $output = dotnet test OpenScrape.sln --no-build --verbosity quiet 2>&1 | Out-String
    if ($output -notmatch "Passed!") { throw "Tests failed" }
    if ($output -match "Failed:\s*0") { return }
    throw "Some tests failed"
}

# Checkpoint 5: Warnings
Test-Checkpoint "Warnings" {
    $output = dotnet build OpenScrape.sln 2>&1 | Out-String
    $warningLines = $output -split "`n" | Where-Object { $_ -match "warning" }
    $count = ($warningLines | Measure-Object).Count
    if ($count -gt $MaxWarnings) { throw "Too many warnings: $count (max: $MaxWarnings)" }
    if ($Verbose) {
        Write-Host "  Warnings: $count" -ForegroundColor Yellow
    }
}

# Checkpoint 6: csproj changes
Test-Checkpoint "No csproj Changes" {
    $output = git diff main --stat -- "**/*.csproj" 2>&1 | Out-String
    if ($output -match "\d+\s+files? changed") { throw "csproj files changed" }
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
$failed = $results | Where-Object { $_.Status -eq "FAIL" }
if ($failed) {
    Write-Host "FAILED: $($failed.Count) checkpoints" -ForegroundColor Red
    Write-Host "Elapsed: $($elapsed.TotalSeconds)s" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "ALL CHECKPOINTS PASSED" -ForegroundColor Green
    Write-Host "Elapsed: $($elapsed.TotalSeconds)s" -ForegroundColor Yellow
    exit 0
}