# Corrige el historial de migraciones (error: "DevFlowRuns already exists").
# Ejecutar desde la raíz del proyecto.
# Ejemplo: .\fix-migrations.ps1

$ErrorActionPreference = "Stop"
Write-Host "Corrigiendo historial de migraciones..." -ForegroundColor Cyan
dotnet run --project src\Gateway.Api -- --fix-migration-history
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nListo. Puedes iniciar la API normalmente." -ForegroundColor Green
} else {
    Write-Host "`nError al ejecutar. Verifica que PostgreSQL esté en marcha." -ForegroundColor Red
    exit $LASTEXITCODE
}
