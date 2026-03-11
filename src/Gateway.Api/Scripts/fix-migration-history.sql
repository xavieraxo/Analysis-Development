-- Script para corregir el historial de migraciones cuando las tablas ya existen.
-- Ejecutar si aparece error "relation 'DevFlowRuns' already exists" (o similar).
--
-- OPCIÓN 1 - Docker (sin psql instalado):
--   Desde la raíz del proyecto: .\src\Gateway.Api\Scripts\run-fix-migrations.ps1
--
-- OPCIÓN 2 - psql (si está en PATH):
--   psql -h localhost -p 5433 -U appuser -d multiagent -f fix-migration-history.sql
--
-- OPCIÓN 3 - pgAdmin/DBeaver: abrir este archivo y ejecutarlo.

-- Inserción segura: ignora si ya existe.
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
  ('20260114160210_InitialPostgres', '9.0.0'),
  ('20260305011351_AddDevFlowRun', '9.0.0'),
  ('20260305011954_AddDevFlowArtifact', '9.0.0'),
  ('20260305123528_AddDevFlowGate', '9.0.0'),
  ('20260305201719_AddBranchPlanModels', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
