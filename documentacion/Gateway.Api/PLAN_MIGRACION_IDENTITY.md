# Plan de Migración a ASP.NET Core Identity

## Fecha de Inicio: 18 de diciembre de 2025

## Objetivo
Migrar gradualmente el sistema de autenticación actual (BCrypt personalizado) a ASP.NET Core Identity sin pérdida de datos.

## Estado de las Fases

### ✅ FASE 1: Preparación y Backup - COMPLETADA
- [x] Crear branch de backup: `backup-pre-identity`
- [x] Backup de base de datos (PostgreSQL) con `pg_dump`
- [x] Documento de seguimiento creado

### ✅ FASE 2: Instalación de Paquetes - COMPLETADA
- [x] Agregar paquetes NuGet de Identity
- [x] Mantener BCrypt
- [x] Restaurar paquetes

### ✅ FASE 3: Crear Modelos de Identity - COMPLETADA
- [x] Crear ApplicationUser.cs
- [x] Crear ApplicationRole.cs

### ✅ FASE 4: Actualizar DbContext - COMPLETADA
- [x] Modificar ApplicationDbContext para coexistencia
- [x] Configurar tablas duales

### ✅ FASE 5: Generar Migración - COMPLETADA
- [x] Generar migraciones EF Core para tablas Identity
- [x] Tablas creadas en base de datos (PostgreSQL)
- [x] Datos existentes preservados

### ✅ FASE 6: Script de Migración de Datos - COMPLETADA
- [x] Crear UserMigrationService
- [x] Crear endpoint de migración
- [x] Registrar servicio

### ✅ FASE 7: Configurar Identity - COMPLETADA
- [x] Agregar configuración Identity en Program.cs
- [x] Mantener AuthService actual
- [x] Sistema compilando correctamente

### ⚠️ FASE 8: Ejecutar Migración de Datos - REQUIERE ACCIÓN MANUAL
- [ ] Ejecutar aplicación
- [ ] Llamar endpoint de migración
- [ ] Guardar contraseñas temporales
**👉 Ver INSTRUCCIONES_MIGRACION.md para pasos detallados**

### ✅ FASE 9: AuthServiceIdentity - COMPLETADA
- [x] Crear AuthServiceIdentity.cs
- [x] Configurar switch UseIdentityAuth
- [x] Actualizar appsettings.json

### ⏳ FASE 10: Testing - PENDIENTE (Después de Fase 8)
- [ ] Activar UseIdentityAuth
- [ ] Probar login
- [ ] Probar registro
- [ ] Probar cambio de contraseña
- [ ] Probar bloqueo de cuenta

### ⏳ FASE 11: Limpieza Final - PENDIENTE (Opcional)
- [ ] Actualizar relaciones de Project
- [ ] Eliminar tabla Users antigua
- [ ] Remover BCrypt

## Notas Importantes

### Backups Creados
- Branch Git: `backup-pre-identity`
- Base de datos: `pg_dump` (archivo `.dump`)

### Reversión
Si algo sale mal en cualquier fase:
```bash
git checkout backup-pre-identity
```

### Usuarios Migrados
Las contraseñas temporales se guardarán en la respuesta del endpoint `/api/admin/migrate-users`.

## Próximos Pasos
Continuar con FASE 8: Ejecutar Migración de Datos

