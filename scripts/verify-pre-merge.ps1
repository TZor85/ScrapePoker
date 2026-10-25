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
    # Sin --no-build a proposito: el checkpoint anterior compila Release con
    # --no-incremental y reescribe archivos de obj/ que el test con --no-build
    # lee a la vez, lo que produjo un fallo intermitente. Un build incremental
    # aca cuesta segundos y garantiza que el binario probado sea el actual.
    dotnet test OpenScrape.sln --verbosity quiet 2>&1 | Out-Null
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

# Checkpoint 6: csproj changes (informativo)
# Un repo que actualiza dependencias con normalidad (por ejemplo Marten 8 -> 9
# por CVE-2026-75513) no puede tener un gate que falle ante cualquier cambio de
# csproj: seria un falso positivo permanente. Se reporta el diff para que el
# revisor lo vea, pero el gate real de paquetes vive en el checkpoint 5 (NU190x)
# y en el CI.
# El baseline es el merge-base con develop, la rama destino del pre-merge.
# Comparar contra main daba ruido: main es una rama vieja que ni siquiera
# contiene todos los proyectos.
Write-Host "=== csproj diff (informativo) ===" -ForegroundColor Cyan
$developRef = $null
git show-ref --verify --quiet refs/heads/develop
if ($LASTEXITCODE -eq 0) { $developRef = "develop" }
if (-not $developRef) {
    git show-ref --verify --quiet refs/remotes/origin/develop
    if ($LASTEXITCODE -eq 0) { $developRef = "origin/develop" }
}

$csprojChanged = @()
$baselineRef = $null
if ($developRef) {
    $mergeBase = (git merge-base HEAD $developRef 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -eq 0 -and $mergeBase -match '^[0-9a-f]{7,40}$') {
        $baselineRef = $mergeBase
        $csprojChanged = @(git diff --name-only $mergeBase -- "*.csproj")
    }
}

if (-not $baselineRef) {
    Write-Host "  No se pudo calcular el baseline con develop: revisar los csproj a mano" -ForegroundColor Yellow
} elseif ($csprojChanged.Count -eq 0) {
    Write-Host "  Sin cambios de csproj respecto de $developRef" -ForegroundColor Green
} else {
    Write-Host ("  {0} csproj modificados respecto de {1}:" -f $csprojChanged.Count, $developRef) -ForegroundColor Yellow
    $csprojChanged | ForEach-Object { Write-Host "    $_" }
}
$script:results += @{ Name = "csproj diff (informativo, no falla)"; Status = "INFO" }

# Checkpoint de paridad: los fixtures congelan la decision del motor antes de
# moverlo a la API. Si desaparecen, el replay de la suite no tiene contrato que
# comparar y la paridad de las fases siguientes queda sin evidencia.
Write-Host "=== Fixtures de paridad ===" -ForegroundColor Cyan
$preflopCount = @(Get-ChildItem "test/fixtures/decisions/preflop" -Filter "*.json" -ErrorAction SilentlyContinue).Count
$postflopCount = @(Get-ChildItem "test/fixtures/decisions/postflop" -Filter "*.json" -ErrorAction SilentlyContinue).Count
Write-Host ("  preflop: {0}  |  postflop: {1}" -f $preflopCount, $postflopCount)

if ($preflopCount -eq 0 -or $postflopCount -eq 0) {
    $script:results += @{
        Name = "Fixtures de paridad"
        Status = "FAIL"
        Error = "Faltan fixtures (preflop=$preflopCount, postflop=$postflopCount). Correr los generadores [Explicit]."
    }
} elseif ($preflopCount -lt 8 -or $postflopCount -lt 7) {
    Write-Host "  Aviso: se esperaban al menos 8 preflop y 7 postflop" -ForegroundColor Yellow
    $script:results += @{ Name = "Fixtures de paridad"; Status = "INFO" }
} else {
    $script:results += @{ Name = "Fixtures de paridad"; Status = "PASS" }
}

# Checkpoint de arquitectura: el contrato compartido con la API no puede
# arrastrar Marten ni ASP.NET, o deja de poder publicarse como ensamblado aparte.
Test-Checkpoint "Arquitectura de Contracts" {
    dotnet test "test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj" --no-build --filter "FullyQualifiedName~Architecture" --nologo 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "El contrato compartido dejo de estar aislado (ver ContractsIsolationTests)." }
}

# Checkpoint de publish: el cliente no puede llevar el motor ni los use cases
# de Marten en el binario. Publica en un directorio temporal y falla si aparece
# cualquiera de los dos ensamblados.
Test-Checkpoint "Publish sin motor" {
    $publishDir = Join-Path ([System.IO.Path]::GetTempPath()) ("openscrape-publish-check-" + [Guid]::NewGuid().ToString("N"))
    try {
        dotnet publish "src/OpenScrape.App/OpenScrape.App.csproj" -c Release -r win-x64 --self-contained false -o $publishDir --nologo 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "El publish del cliente fallo." }

        if (Test-Path (Join-Path $publishDir "OpenScrape.DecisionMaker.dll")) {
            throw "El publish todavia incluye OpenScrape.DecisionMaker.dll."
        }

        if (Test-Path (Join-Path $publishDir "OpenScrape.Features.dll")) {
            throw "El publish todavia incluye OpenScrape.Features.dll."
        }
    }
    finally {
        if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
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
    $color = switch ($_.Status) {
        "PASS" { "Green" }
        "INFO" { "Cyan" }
        default { "Red" }
    }
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