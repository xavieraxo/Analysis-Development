# 📋 Instrucciones para Completar la Migración a Identity

## ✅ Fases Completadas (1-9)

Has completado exitosamente las primeras 9 fases de la migración gradual a ASP.NET Core Identity:

- ✅ **Fase 1:** Backup y preparación
- ✅ **Fase 2:** Instalación de paquetes
- ✅ **Fase 3:** Modelos de Identity creados
- ✅ **Fase 4:** DbContext configurado para coexistencia
- ✅ **Fase 5:** Tablas de Identity creadas en BD
- ✅ **Fase 6:** UserMigrationService implementado
- ✅ **Fase 7:** Identity configurado en Program.cs
- ✅ **Fase 8:** (Pendiente ejecución manual)
- ✅ **Fase 9:** AuthServiceIdentity con switch creado

---

## 🔄 FASE 8: Ejecutar Migración de Datos (ACCIÓN REQUERIDA)

Esta fase debe ejecutarse **manualmente** para migrar los usuarios existentes de la tabla `Users` (BCrypt) a la tabla `IdentityUsers` (Identity).

### Pasos para Ejecutar la Migración:

#### 1. Iniciar la Aplicación

```powershell
cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Api
dotnet run
```

Deberías ver en consola:
```
✅ Usando AuthService original (BCrypt)
Now listening on: http://localhost:5000
```

#### 2. Abrir Swagger

Navega a: `http://localhost:5000/swagger` (o el puerto que muestre la consola)

#### 3. Hacer Login como Admin/SuperUsuario

En Swagger, busca `POST /api/auth/login` y ejecuta:

```json
{
  "email": "tu_email_admin@example.com",
  "password": "tu_contraseña"
}
```

O usa el SuperUser hash si lo tienes configurado:

```json
{
  "email": "A4F1C72E99B3842F7D1A5C0083F6D2B1",
  "password": "cualquier_valor"
}
```

**Copia el token JWT** que devuelve la respuesta.

#### 4. Autorizar en Swagger

1. Haz clic en el botón **"Authorize"** (arriba a la derecha en Swagger)
2. Ingresa: `Bearer TU_TOKEN_JWT_AQUI`
3. Haz clic en "Authorize"

#### 5. Ejecutar la Migración

Busca el endpoint `POST /api/admin/migrate-users` y ejecútalo.

**Respuesta Esperada:**

```json
{
  "migrated": 5,
  "skipped": 0,
  "failed": 0,
  "tempPasswords": [
    "user1@example.com -> Temp1!Abc",
    "user2@example.com -> Temp2!Abc",
    "admin@example.com -> Temp3!Abc"
  ],
  "errors": []
}
```

#### 6. ⚠️ IMPORTANTE: Guardar las Contraseñas Temporales

**¡MUY IMPORTANTE!** Copia y guarda las contraseñas temporales en un lugar seguro. Los usuarios migrados necesitarán estas contraseñas para hacer login con el nuevo sistema Identity.

**Ejemplo de archivo:** `CONTRASEÑAS_TEMPORALES.txt`
```
user1@example.com -> Temp1!Abc
user2@example.com -> Temp2!Abc
admin@example.com -> Temp3!Abc
```

#### 7. Verificar la Migración

Puedes verificar que los usuarios se migraron correctamente ejecutando una consulta SQL en PostgreSQL:

```sql
SELECT COUNT(*) as TotalOriginal FROM "Users";
SELECT COUNT(*) as TotalMigrado FROM "IdentityUsers";
SELECT "Email", "Name", "Role", "IsActive" FROM "IdentityUsers";
```

---

## 🔄 FASE 10: Activar Identity Auth (Después de Migración Exitosa)

Una vez que la migración se completó exitosamente, puedes activar el nuevo sistema de autenticación:

### 1. Detener la Aplicación

Presiona `Ctrl+C` en la terminal donde corre `dotnet run`.

### 2. Editar appsettings.json

Cambia el flag `UseIdentityAuth` a `true`:

```json
{
  ...
  "UseIdentityAuth": true
}
```

### 3. Reiniciar la Aplicación

```powershell
dotnet run
```

Deberías ver:
```
✅ Usando AuthServiceIdentity (ASP.NET Core Identity)
```

### 4. Probar Login con Identity

#### A. Con Contraseñas Temporales

En Swagger, `POST /api/auth/login`:

```json
{
  "email": "user1@example.com",
  "password": "Temp1!Abc"
}
```

Debería funcionar y devolver un token JWT.

#### B. Cambiar Contraseña (Recomendado)

Usa el endpoint `POST /api/auth/change-password` para que los usuarios cambien sus contraseñas temporales:

```json
{
  "currentPassword": "Temp1!Abc",
  "newPassword": "MiNuevaContraseña123!"
}
```

---

## ✅ FASE 11: Limpieza Final (OPCIONAL - Solo cuando TODO funcione)

**⚠️ ADVERTENCIA:** Esta fase elimina la tabla `Users` antigua. Solo ejecutar cuando estés 100% seguro que Identity funciona correctamente.

### Pasos:

#### 1. Backup Final

```powershell
pg_dump -h localhost -p 5433 -U appuser -d multiagent -F c -f "multiagent.before-cleanup-$(Get-Date -Format 'yyyyMMdd-HHmmss').dump"
```

#### 2. Eliminar Referencia a User en Program.cs

Comentar o eliminar estas líneas si ya no se usan.

#### 3. Eliminar la Tabla Users (SQL)

Ejecuta en PostgreSQL:

```sql
-- Verificar que IdentityUsers tiene todos los usuarios
SELECT COUNT(*) FROM "IdentityUsers";

-- Si todo está bien, eliminar tabla Users
DROP TABLE IF EXISTS "Users";
```

#### 4. Eliminar AuthService.cs (Sistema Antiguo)

```powershell
Remove-Item E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Api\Services\AuthService.cs
```

#### 5. Remover BCrypt del proyecto

Editar `Gateway.Api.csproj` y eliminar:
```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
```

#### 6. Renombrar AuthServiceIdentity a AuthService (Opcional)

Para simplificar, puedes renombrar `AuthServiceIdentity` a `AuthService` ya que será el único sistema.

---

## 📊 Checklist de Validación

Antes de considerar la migración completa, verifica:

- [ ] Todos los usuarios se migraron exitosamente
- [ ] Login funciona con contraseñas temporales
- [ ] Registro de nuevos usuarios funciona
- [ ] Cambio de contraseña funciona
- [ ] Bloqueo de cuenta después de 5 intentos fallidos funciona
- [ ] Todos los endpoints protegidos funcionan correctamente
- [ ] Los proyectos de usuarios siguen accesibles

---

## 🔄 Reversión de Emergencia

Si algo sale mal en cualquier momento:

### Opción 1: Volver a BCrypt

```json
{
  "UseIdentityAuth": false
}
```

Reinicia la aplicación y todo volverá a funcionar con el sistema anterior.

### Opción 2: Restaurar Backup

```powershell
pg_restore -h localhost -p 5433 -U appuser -d multiagent -c "multiagent.backup_YYYYMMDD_HHMMSS.dump"
```

### Opción 3: Checkout del Branch de Backup

```bash
git checkout backup-pre-identity
```

---

## 📝 Notas Importantes

1. **Contraseñas Temporales:** Los usuarios NO pueden usar sus contraseñas antiguas después de la migración. DEBEN usar las contraseñas temporales generadas.

2. **Comunicación a Usuarios:** Informa a tus usuarios sobre:
   - Las nuevas contraseñas temporales
   - La necesidad de cambiarlas al primer login
   - Las nuevas políticas de contraseña (mínimo 8 caracteres, etc.)

3. **Tabla Users Original:** Se mantiene intacta durante toda la migración. Solo se elimina en la Fase 11 (opcional).

4. **Sistema Dual:** Puedes alternar entre BCrypt e Identity en cualquier momento cambiando `UseIdentityAuth`.

---

## 🆘 Solución de Problemas

### Error: "Usuario no encontrado"

- Verifica que la migración se ejecutó: `SELECT * FROM IdentityUsers;`
- Asegúrate de que `UseIdentityAuth: true` en appsettings.json

### Error: "Contraseña incorrecta"

- Estás usando la contraseña temporal? Verifica las contraseñas en la respuesta de `/api/admin/migrate-users`

### Error: "Cuenta bloqueada"

- Después de 5 intentos fallidos, la cuenta se bloquea por 5 minutos
- Espera o desbloquea manualmente en la BD: `UPDATE IdentityUsers SET LockoutEnd = NULL WHERE Email = 'user@example.com'`

---

## ✅ Siguiente Paso

**Ejecuta la FASE 8 siguiendo las instrucciones arriba.**

Una vez completada exitosamente, podrás activar Identity Auth (Fase 10) y eventualmente limpiar el sistema antiguo (Fase 11).

