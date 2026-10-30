<#
.SYNOPSIS
    Publica la API y la instala como servicio de Windows con arranque automático.

.DESCRIPTION
    Deja la API corriendo en segundo plano desde el arranque del sistema, con
    reinicio automático si se cae. Se puede volver a ejecutar las veces que haga
    falta: detiene y recrea el servicio.

    Los secretos se leen de un archivo con formato CLAVE=valor (por defecto
    .env.dev, el mismo que usa scripts/dev-run.ps1) y se copian a las variables
    de entorno de máquina, que es lo que ve la cuenta LocalSystem del servicio.

    Requiere consola elevada. La publicacion es self-contained (win-x64), asi que la
    maquina destino no necesita tener .NET instalado.

.EXAMPLE
    .\scripts\install-api-service.ps1

.EXAMPLE
    .\scripts\install-api-service.ps1 -EnvFile C:\secrets\prod.env -Port 5015

.EXAMPLE
    .\scripts\install-api-service.ps1 -SkipPublish
#>
[CmdletBinding()]
param(
    [string] $ServiceName = 'OpenScrapeApi',
    [string] $InstallPath = 'C:\Services\OpenScrapeApi',
    [int] $Port = 5015,
    [string] $EnvFile,
    [switch] $SkipPublish
)

$ErrorActionPreference = 'Stop'

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal $identity
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Este script necesita una consola elevada (Ejecutar como administrador).'
    }
}

Assert-Administrator

$root = Split-Path -Parent $PSScriptRoot
if (-not $EnvFile) {
    $EnvFile = Join-Path $root '.env.dev'
}

if (-not (Test-Path $EnvFile)) {
    throw "Falta $EnvFile. Copia .env.dev.example y completa los secretos."
}

if (-not $SkipPublish) {
    Write-Host "Publicando la API en $InstallPath ..." -ForegroundColor Cyan
    $project = Join-Path $root 'src/OpenScrape.Api/OpenScrape.Api.csproj'
    & dotnet publish $project -c Release -r win-x64 --self-contained true -o $InstallPath --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish fallo con codigo $LASTEXITCODE."
    }
}

$exe = Join-Path $InstallPath 'OpenScrape.Api.exe'
if (-not (Test-Path $exe)) {
    throw "No se encontro $exe. Ejecuta el script sin -SkipPublish."
}

# Servicio previo: fuera, para que la reinstalacion sea limpia.
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Deteniendo el servicio $ServiceName ..." -ForegroundColor Cyan
    if ($existing.Status -ne 'Stopped') {
        Stop-Service -Name $ServiceName -Force
        $existing.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(15))
    }

    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Secretos a variables de maquina: LocalSystem no ve las de usuario.
Write-Host "Cargando variables desde $EnvFile ..." -ForegroundColor Cyan
Get-Content $EnvFile | ForEach-Object {
    $line = $_.Trim()
    if (-not $line -or $line.StartsWith('#')) { return }

    $separator = $line.IndexOf('=')
    if ($separator -lt 1) { return }

    $name = $line.Substring(0, $separator).Trim()
    $value = $line.Substring($separator + 1).Trim().Trim('"')
    [Environment]::SetEnvironmentVariable($name, $value, 'Machine')
    [Environment]::SetEnvironmentVariable($name, $value, 'Process')
}

Write-Host "Creando el servicio $ServiceName ..." -ForegroundColor Cyan
New-Service -Name $ServiceName `
    -BinaryPathName "`"$exe`" --urls http://localhost:$Port" `
    -DisplayName 'OpenScrape API' `
    -Description 'Motor de decision de OpenScrape' `
    -StartupType Automatic | Out-Null

# Reinicio automatico: 5 s, 15 s y 60 s entre intentos.
& sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/15000/restart/60000 | Out-Null
& sc.exe start $ServiceName | Out-Null

Write-Host "Esperando a que /health responda ..." -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds(30)
$healthy = $false
while ((Get-Date) -lt $deadline) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port/health" -UseBasicParsing -TimeoutSec 3
        if ($response.StatusCode -eq 200) {
            $healthy = $true
            break
        }
    }
    catch {
        Start-Sleep -Milliseconds 500
    }
}

if (-not $healthy) {
    Write-Host 'El servicio no respondio /health. Ultimos eventos de la aplicacion:' -ForegroundColor Red
    Get-WinEvent -LogName Application -MaxEvents 20 -ErrorAction SilentlyContinue |
        Where-Object { $_.ProviderName -in '.NET Runtime', 'Application Error' } |
        Select-Object -First 10 TimeCreated, ProviderName, Message |
        Format-Table -Wrap

    throw "El servicio $ServiceName no quedo sano en el puerto $Port."
}

Write-Host ''
Write-Host "Servicio $ServiceName activo en http://localhost:$Port" -ForegroundColor Green
Write-Host "Configura en el cliente: Api:BaseUrl = http://localhost:$Port"
