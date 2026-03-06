# Ejecuta fix-migration-history.sql usando el contenedor PostgreSQL de Docker.
# Desde la raíz del proyecto (E:\Proyectos\PoC_Analisis_Desarrollo):
#
#   .\src\Gateway.Api\Scripts\run-fix-migrations.ps1
#
# Requiere: Docker en marcha y el servicio postgres corriendo (docker compose up -d).

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFile = Join-Path $scriptDir "fix-migration-history.sql"
$projectRoot = Resolve-Path (Join-Path $scriptDir "..\..\..")

if (-not (Test-Path $sqlFile)) {
    Write-Error "No se encuentra: $sqlFile"
}

Push-Location $projectRoot
try {
    Get-Content $sqlFile -Raw | docker compose -f infra/docker-compose.yml exec -T postgres psql -U appuser -d multiagent
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Script ejecutado correctamente." -ForegroundColor Green
    } else {
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}
