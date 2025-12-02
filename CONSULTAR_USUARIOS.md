# Cómo Consultar Usuarios en la Base de Datos

## Estado Actual

La base de datos SQLite se crea automáticamente cuando ejecutas **Gateway.Api** por primera vez.

**Ubicación de la base de datos:** `src/Gateway.Api/multiagent.db`

## Cómo se Crean los Usuarios

### 1. SuperUsuario (Automático)
El SuperUsuario se crea **automáticamente** cuando:
- Haces login por primera vez usando el hash configurado en `appsettings.json`
- Hash configurado: `A4F1C72E99B3842F7D1A5C0083F6D2B1`

**Datos del SuperUsuario cuando se crea:**
- Email: `superuser@system.local`
- Name: `Super Usuario`
- Role: `SuperUsuario`
- Password: `superuser` (hasheado con BCrypt)

### 2. Usuarios Normales
Los usuarios normales se crean:
- Mediante el endpoint `/api/auth/register` (registro público)
- Mediante el endpoint `/api/admin/users` (creación por Admin/SuperUsuario)

## Métodos para Consultar Usuarios

### Método 1: Usar la API (Recomendado si estás autenticado)

1. **Login como SuperUsuario:**
   ```
   POST http://localhost:8096/api/auth/login
   {
     "email": "A4F1C72E99B3842F7D1A5C0083F6D2B1",
     "password": ""
   }
   ```

2. **Obtener todos los usuarios:**
   ```
   GET http://localhost:8096/api/admin/users
   Authorization: Bearer {token_del_login}
   ```

### Método 2: Usar Swagger (MÁS FÁCIL - Recomendado)

1. **Abre tu navegador** y ve a: `http://localhost:8096/swagger`

2. **Haz login como SuperUsuario:**
   - Busca el endpoint `POST /api/auth/login`
   - Haz clic en "Try it out"
   - Ingresa en el campo `email`: `A4F1C72E99B3842F7D1A5C0083F6D2B1`
   - Deja el campo `password` vacío: `""`
   - Haz clic en "Execute"
   - **Copia el token** que aparece en la respuesta (campo `token`)

3. **Obtener todos los usuarios:**
   - Busca el endpoint `GET /api/admin/users`
   - Haz clic en "Try it out"
   - Haz clic en el botón "Authorize" (arriba a la derecha)
   - Pega el token que copiaste en el campo "Value"
   - Haz clic en "Authorize" y luego "Close"
   - Haz clic en "Execute" en el endpoint `GET /api/admin/users`
   - Verás la lista completa de usuarios en formato JSON

### Método 3: Consultar Base de Datos Directamente (SQLite - Requiere instalación)

Si tienes SQLite instalado en Windows, puedes usar PowerShell:

```powershell
# Instalar SQLite (si no lo tienes)
# Descarga desde: https://www.sqlite.org/download.html
# O instala desde Chocolatey: choco install sqlite

# Ejecutar consulta
sqlite3.exe src\Gateway.Api\multiagent.db "SELECT Id, Email, Name, Role, IsActive, CreatedAt FROM Users;"
```

### Método 3: Desde la Interfaz Blazor

Si estás autenticado como SuperUsuario o Admin:
1. Ve a `/admin/users` en el navegador
2. Verás la lista de usuarios en la interfaz

## Estructura de la Tabla Users

| Campo | Tipo | Descripción |
|-------|------|-------------|
| Id | int | ID único del usuario |
| Email | string | Email del usuario (único) |
| PasswordHash | string | Hash de la contraseña (BCrypt) |
| Name | string | Nombre del usuario |
| Role | int (enum) | Rol del usuario: 0=Final, 1=Empresa, 2=Admin, 3=SuperUsuario |
| IsActive | bool | Si el usuario está activo |
| CreatedAt | DateTime | Fecha de creación |
| LastLoginAt | DateTime? | Última fecha de login |

## Roles de Usuario

- **Final (0)**: Usuario final normal
- **Empresa (1)**: Usuario tipo empresa
- **Admin (2)**: Administrador
- **SuperUsuario (3)**: Super usuario (acceso con hash)

## Nota Importante

Si la base de datos no existe aún, simplemente **ejecuta Gateway.Api una vez** y la base de datos se creará automáticamente.

