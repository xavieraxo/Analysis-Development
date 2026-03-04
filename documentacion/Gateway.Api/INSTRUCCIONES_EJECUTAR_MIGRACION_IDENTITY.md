# 🔄 **INSTRUCCIONES: Ejecutar Migración a ASP.NET Core Identity**

**Fecha:** 19 de diciembre de 2025  
**Estado:** ✅ TODO PREPARADO - Listo para ejecutar

---

## ⚠️ **IMPORTANTE - LEER ANTES DE EJECUTAR**

Esta migración es **REVERSIBLE** y se ejecuta en **MODO COEXISTENCIA**:
- ✅ La tabla `Users` original **NO se elimina**
- ✅ Puedes volver atrás cambiando `UseIdentityAuth: false`
- ✅ Se crea un backup automático de la base de datos
- ✅ SuperAdmin sigue funcionando con hash

---

## 📋 **PREREQUISITOS**

### **1. Verificar que todo esté compilado:**
```powershell
cd E:\Proyectos\PoC_Analisis_Desarrollo
dotnet build
```

### **2. Verificar backup de base de datos:**
```powershell
# Ejemplo usando pg_dump (PostgreSQL)
pg_dump -h localhost -p 5433 -U appuser -d multiagent -F c -f "multiagent.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').dump"
```

Deberías ver un archivo `.dump` generado.

---

## 🚀 **PASO 1: INICIAR LA APLICACIÓN**

### **Opción A: Desde Visual Studio**
1. Abrir solución en Visual Studio
2. Establecer `Gateway.Api` como proyecto de inicio
3. Presionar **F5** o **Ctrl+F5**
4. Esperar a que aparezca Swagger UI

### **Opción B: Desde Terminal**
```powershell
cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Api
dotnet run --no-launch-profile --urls "http://localhost:8096"
```

**Verificar que esté corriendo:**
- Abrir navegador: `http://localhost:8096/swagger`
- Deberías ver la documentación de Swagger

---

## 🔐 **PASO 2: OBTENER TOKEN DE SUPERADMIN**

### **2.1. Abrir Blazor Frontend:**
```powershell
# En otra terminal
cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Blazor
dotnet run --no-launch-profile --urls "http://localhost:8095"
```

### **2.2. Hacer Login como SuperAdmin:**
1. Abrir navegador: `http://localhost:8095`
2. Ir a Login
3. Hacer clic en "¿Eres SuperAdministrador?"
4. Pegar el hash SuperAdmin del `appsettings.json`:
   ```
   A4F1C72E99B3842F7D1A5C0083F6D2B1
   ```
5. (Opcional) Ingresa una contraseña si deseas crear el SuperUsuario con esa clave
6. Hacer clic en "Iniciar Sesión"

### **2.3. Obtener Token JWT:**
**Opción A: Desde DevTools del navegador**
1. Presionar **F12** para abrir DevTools
2. Ir a pestaña **Console**
3. Ejecutar:
   ```javascript
   localStorage.getItem('authToken')
   ```
4. Copiar el token (sin comillas)

**Opción B: Desde Application Storage**
1. Presionar **F12** para abrir DevTools
2. Ir a pestaña **Application** (o **Almacenamiento**)
3. En el panel izquierdo: **Local Storage** → `http://localhost:8095`
4. Buscar clave `authToken`
5. Copiar el valor

---

## 🔄 **PASO 3: EJECUTAR MIGRACIÓN**

### **3.1. Abrir Swagger UI:**
- Navegador: `http://localhost:8096/swagger`

### **3.2. Autorizar con Token:**
1. Hacer clic en el botón **"Authorize"** (candado verde en la esquina superior derecha)
2. En el campo **Value**, pegar:
   ```
   Bearer TU_TOKEN_AQUI
   ```
   ⚠️ **IMPORTANTE:** Debe empezar con `Bearer ` (con espacio)
3. Hacer clic en **"Authorize"**
4. Hacer clic en **"Close"**

### **3.3. Ejecutar Endpoint de Migración:**
1. Buscar el endpoint: **`POST /api/admin/migrate-users`**
2. Hacer clic en el endpoint para expandirlo
3. Hacer clic en **"Try it out"**
4. Hacer clic en **"Execute"**

### **3.4. Verificar Respuesta:**

**✅ ÉXITO (Status 200):**
```json
{
  "success": true,
  "totalUsers": 3,
  "migratedUsers": 3,
  "skippedUsers": 0,
  "errors": []
}
```

**❌ ERROR (Status 400/500):**
```json
{
  "success": false,
  "totalUsers": 3,
  "migratedUsers": 1,
  "skippedUsers": 2,
  "errors": [
    "Usuario admin@example.com ya existe en Identity"
  ]
}
```

---

## ⚙️ **PASO 4: ACTIVAR SISTEMA IDENTITY**

### **4.1. Detener la Aplicación:**
- Presionar **Ctrl+C** en las terminales de Backend y Frontend

### **4.2. Editar appsettings.json:**
```powershell
# Abrir archivo
notepad E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Api\appsettings.json
```

**Cambiar:**
```json
"UseIdentityAuth": false
```

**Por:**
```json
"UseIdentityAuth": true
```

**Guardar y cerrar.**

### **4.3. Reiniciar Aplicación:**
```powershell
# Terminal 1 - Backend
cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Api
dotnet run --no-launch-profile --urls "http://localhost:8096"

# Terminal 2 - Frontend
cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Blazor
dotnet run --no-launch-profile --urls "http://localhost:8095"
```

---

## ✅ **PASO 5: PROBAR SISTEMA IDENTITY**

### **5.1. Probar Login con Usuario Migrado:**
1. Abrir: `http://localhost:8095`
2. Ir a Login
3. Ingresar credenciales de un usuario existente:
   - Email: `admin@example.com`
   - Password: La contraseña original del usuario
4. Hacer clic en "Iniciar Sesión"

**✅ Esperado:** Login exitoso, redirige al Dashboard

### **5.2. Probar SuperAdmin (debe seguir funcionando):**
1. Hacer logout
2. Ir a Login
3. Hacer clic en "¿Eres SuperAdministrador?"
4. Pegar el hash SuperAdmin
5. Hacer clic en "Iniciar Sesión"

**✅ Esperado:** Login exitoso como SuperAdmin

### **5.3. Probar Crear Nuevo Usuario:**
1. Como SuperAdmin, ir a **Admin Usuarios**
2. Hacer clic en **"Nuevo Usuario"**
3. Llenar formulario:
   - Email: `test@identity.com`
   - Nombre: `Test Identity`
   - Password: `Test123@`
   - Rol: Final
4. Hacer clic en **"Crear"**

**✅ Esperado:** Usuario creado exitosamente

### **5.4. Probar Login con Nuevo Usuario:**
1. Hacer logout
2. Ir a Login
3. Ingresar:
   - Email: `test@identity.com`
   - Password: `Test123@`
4. Hacer clic en "Iniciar Sesión"

**✅ Esperado:** Login exitoso

### **5.5. Probar Cambiar Contraseña:**
1. Estando logueado, hacer clic en el avatar (esquina superior derecha)
2. Hacer clic en **"Perfil"**
3. Hacer clic en **"Cambiar Contraseña"**
4. Llenar formulario:
   - Contraseña Actual: `Test123@`
   - Nueva Contraseña: `NewTest123@`
   - Confirmar: `NewTest123@`
5. Hacer clic en **"Cambiar Contraseña"**

**✅ Esperado:** 
- Mensaje de éxito
- Logout automático
- Puede hacer login con nueva contraseña

---

## 🔙 **ROLLBACK (Si algo sale mal)**

### **Opción 1: Desactivar Identity (Rápido)**
1. Detener aplicación
2. Editar `appsettings.json`:
   ```json
   "UseIdentityAuth": false
   ```
3. Reiniciar aplicación
4. **Resultado:** Vuelve al sistema antiguo con BCrypt

### **Opción 2: Restaurar Backup de BD (Completo)**
```powershell
# Detener aplicación primero
# Luego restaurar backup con pg_restore
pg_restore -h localhost -p 5433 -U appuser -d multiagent -c "multiagent.backup_YYYYMMDD_HHMMSS.dump"
```

---

## 📊 **VERIFICAR MIGRACIÓN EN BASE DE DATOS**

### **Usando pgAdmin o psql:**
1. Conectar a `localhost:5433` (DB `multiagent`, usuario `appuser`)
2. Ver tablas:
   - `Users` (tabla original - debe tener datos)
   - `IdentityUsers` (tabla nueva - debe tener usuarios migrados)
   - `IdentityRoles` (tabla nueva - debe tener roles)

### **Consultas SQL de Verificación:**
```sql
-- Ver usuarios originales
SELECT "Id", "Email", "Name", "Role" FROM "Users";

-- Ver usuarios migrados a Identity
SELECT "Id", "UserName", "Email", "NormalizedEmail" FROM "IdentityUsers";

-- Ver relación de migración
SELECT 
    u."Email" as OriginalEmail,
    iu."Email" as IdentityEmail,
    iu."LegacyUserId"
FROM "Users" u
LEFT JOIN "IdentityUsers" iu ON u."Id" = iu."LegacyUserId";
```

---

## 🎯 **CHECKLIST DE PRUEBAS**

- [ ] Backup de BD creado
- [ ] Aplicación compila sin errores
- [ ] Swagger UI accesible
- [ ] Token SuperAdmin obtenido
- [ ] Endpoint de migración ejecutado exitosamente
- [ ] `UseIdentityAuth: true` configurado
- [ ] Login con usuario migrado funciona
- [ ] Login SuperAdmin funciona
- [ ] Crear nuevo usuario funciona
- [ ] Login con nuevo usuario funciona
- [ ] Cambiar contraseña funciona
- [ ] Crear proyecto funciona
- [ ] Roles y permisos funcionan correctamente

---

## ❓ **TROUBLESHOOTING**

### **Error: "Unauthorized" al ejecutar migración**
- **Causa:** Token no válido o expirado
- **Solución:** Obtener nuevo token de SuperAdmin

### **Error: "Usuario ya existe en Identity"**
- **Causa:** Ya ejecutaste la migración antes
- **Solución:** Es normal, los usuarios ya están migrados

### **Error: "No se puede conectar a la base de datos"**
- **Causa:** Archivo de BD bloqueado
- **Solución:** Cerrar DB Browser y reiniciar aplicación

### **Login no funciona después de migración**
- **Causa:** Contraseña no coincide
- **Solución:** 
  1. Volver a `UseIdentityAuth: false`
  2. Verificar contraseña original
  3. Ejecutar migración nuevamente

---

## 📝 **NOTAS IMPORTANTES**

1. **Contraseñas migradas:**
   - Los usuarios migrados deben usar su contraseña **ORIGINAL**
   - Las contraseñas se re-hashean con Identity durante la migración
   - Si no recuerdan la contraseña, crear usuario nuevo

2. **SuperAdmin:**
   - SuperAdmin **NO se migra** a Identity
   - Sigue usando el hash en `appsettings.json`
   - Funciona en ambos sistemas (BCrypt e Identity)

3. **Coexistencia:**
   - Ambas tablas (`Users` e `IdentityUsers`) coexisten
   - El campo `LegacyUserId` vincula usuarios migrados
   - Puedes cambiar entre sistemas sin perder datos

4. **Eliminar tabla Users (Opcional):**
   - Solo después de probar exhaustivamente
   - Hacer backup adicional antes
   - Actualizar `ApplicationDbContext` para eliminar `DbSet<User>`

---

## 🎉 **RESULTADO ESPERADO**

Después de completar todos los pasos:

✅ Sistema de autenticación robusto con ASP.NET Core Identity  
✅ Usuarios migrados funcionando correctamente  
✅ SuperAdmin funcionando  
✅ Cambio de contraseña implementado  
✅ Crear proyecto funcionando  
✅ Todos los roles y permisos funcionando  
✅ Sistema reversible en cualquier momento  

---

**¿Listo para ejecutar?** 🚀

Sigue los pasos en orden y verifica cada uno antes de continuar.

