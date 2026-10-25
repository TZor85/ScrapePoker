<#
.SYNOPSIS
    Carga los secretos de .env.dev y lanza la parte del sistema que le pidas.

.DESCRIPTION
    .env.dev (ignorado por git) es la unica fuente de verdad de los secretos de
    desarrollo: cadena de Neon, firma JWT, pepper de licencias y clave del
    cifrador. Las variables de entorno pisan a los appsettings, asi que la API,
    el cliente y el CLI de licencias ven la misma configuracion sin copiarla.

.EXAMPLE
    .\scripts\dev-run.ps1 api

.EXAMPLE
    .\scripts\dev-run.ps1 app

.EXAMPLE
    .\scripts\dev-run.ps1 license create --plan pro --days 30
#>
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet('api', 'app', 'license', 'test', 'gate')]
    [string] $Target,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $Rest
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root '.env.dev'

if (-not (Test-Path $envFile)) {
    throw "Falta .env.dev. Copia .env.dev.example a .env.dev y completa la conexion de Neon."
}

Get-Content $envFile | ForEach-Object {
    $line = $_.Trim()
    if (-not $line -or $line.StartsWith('#')) { return }
    $separator = $line.IndexOf('=')
    if ($separator -lt 1) { return }

    $name = $line.Substring(0, $separator).Trim()
    $value = $line.Substring($separator + 1).Trim().Trim('"')
    [Environment]::SetEnvironmentVariable($name, $value, 'Process')
}

$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_ENVIRONMENT = 'Development'

function Invoke-InProject {
    param([string] $Project, [scriptblock] $Action)

    Push-Location (Join-Path $root $Project)
    try { & $Action }
    finally { Pop-Location }
}

switch ($Target) {
    'api' {
        Invoke-InProject 'src/OpenScrape.Api' { dotnet run --launch-profile http }
    }
    'app' {
        Invoke-InProject 'src/OpenScrape.App' { dotnet run }
    }
    'license' {
        Invoke-InProject 'src/Tools/LicenseCli' { dotnet run -- @Rest }
    }
    'test' {
        dotnet test (Join-Path $root 'OpenScrape.sln')
    }
    'gate' {
        & (Join-Path $root 'scripts/verify-pre-merge.ps1') -SkipSmoke
    }
}
