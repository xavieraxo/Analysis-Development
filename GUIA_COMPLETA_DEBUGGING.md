# 🎓 GUÍA COMPLETA DE CAPACITACIÓN: DEBUGGING Y EJECUCIÓN DEL PROYECTO

## 📋 ÍNDICE
1. [Arquitectura del Sistema](#1-arquitectura-del-sistema)
2. [Componentes y Comunicación](#2-componentes-y-comunicación)
3. [Preparación del Entorno](#3-preparación-del-entorno)
4. [Ejecución del Proyecto](#4-ejecución-del-proyecto)
5. [Herramientas de Depuración](#5-herramientas-de-depuración)
6. [Verificación y Testing](#6-verificación-y-testing)
7. [Troubleshooting Completo](#7-troubleshooting-completo)
8. [Glosario de Términos](#8-glosario-de-términos)

---

## 1. ARQUITECTURA DEL SISTEMA

### 1.1 Visión General

Este es un **sistema multi-agente** con arquitectura de cliente-servidor:

```
┌─────────────────────────────────────────────────────────────────┐
│                    NAVEGADOR (Cliente)                          │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │        Gateway.Blazor (Blazor Server-Side)               │  │
│  │  - Interfaz de usuario (UI)                              │  │
│  │  - Páginas: Login, Register, Home, Dashboard, etc.       │  │
│  │  - Puerto: 8098                                          │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTP Requests
                         │ (http://localhost:8096)
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              Gateway.Api (ASP.NET Core Minimal API)             │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  - Autenticación (JWT)                                   │  │
│  │  - Endpoints REST (/api/auth/*, /api/projects/*, etc.)   │  │
│  │  - WebSocket (/chat/ws)                                  │  │
│  │  - Puerto: 8096                                          │  │
│  └──────────────────────────────────────────────────────────┘  │
│                         │                                       │
│                         │ Dependencias internas                │
│                         ▼                                       │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  - Orchestrator.App (Coordinador de agentes)             │  │
│  │  - Agents.* (UR, PM, PO, Dev, UX)                        │  │
│  │  - Database: SQLite (multiagent.db)                      │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTP Requests (Opcional)
                         │ (http://localhost:11434/v1)
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              Ollama (Servicio externo - Opcional)               │
│  - Proporciona modelos de lenguaje (LLM)                       │
│  - Puerto: 11434                                                │
│  - Solo necesario para funcionalidad de chat/agentes           │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Proyectos y sus Responsabilidades

#### **Gateway.Blazor** (Frontend)
- **Tipo**: Blazor Server-Side Application
- **Puerto**: `8098`
- **Responsabilidades**:
  - Interfaz de usuario (UI) completa
  - Páginas web (Login, Register, Home, Dashboard, etc.)
  - Comunicación con Gateway.Api mediante HTTP
  - Autenticación del lado del cliente (almacenamiento de tokens)
  - Gestión de estado de autenticación

#### **Gateway.Api** (Backend)
- **Tipo**: ASP.NET Core Minimal API
- **Puerto**: `8096`
- **Responsabilidades**:
  - API REST completa
  - Autenticación JWT
  - Base de datos SQLite
  - Lógica de negocio
  - Coordinación de agentes

#### **Orchestrator.App** (Librería interna)
- **Tipo**: Librería de clases (.NET)
- **No se ejecuta directamente** - es referenciada por Gateway.Api
- **Responsabilidades**:
  - Coordinar el flujo de trabajo entre agentes
  - Gestionar conversaciones multi-agente

#### **Agents.* (UR, PM, PO, Dev, UX)**
- **Tipo**: Librerías de clases (.NET)
- **No se ejecutan directamente** - son referenciadas por Gateway.Api
- **Responsabilidades**:
  - Cada agente tiene un rol específico
  - Procesan mensajes y generan respuestas

#### **Ollama** (Servicio externo)
- **Tipo**: Servicio Docker (opcional)
- **Puerto**: `11434`
- **Responsabilidades**:
  - Proporcionar modelos de lenguaje (LLM)
  - Solo necesario para funcionalidad de chat/agentes
  - **NO es necesario para login/registro/autenticación básica**

---

## 2. COMPONENTES Y COMUNICACIÓN

### 2.1 Flujo de Autenticación (Login)

```
1. Usuario → Navegador
   └─ Accede a http://localhost:8098/login

2. Gateway.Blazor (Login.razor)
   └─ Usuario ingresa email/contraseña o hash de SuperAdmin
   └─ Hace clic en "Iniciar Sesión"

3. AuthService (en Gateway.Blazor)
   └─ Prepara LoginRequest
   └─ Envía POST http://localhost:8096/api/auth/login

4. Gateway.Api (/api/auth/login endpoint)
   └─ Recibe LoginRequest
   └─ Llama a AuthService.LoginAsync() (backend)

5. AuthService (backend)
   └─ Valida credenciales contra base de datos SQLite
   └─ Genera token JWT
   └─ Retorna LoginResponse con token y datos del usuario

6. Gateway.Api
   └─ Retorna LoginResponse al frontend (HTTP 200)

7. AuthService (frontend)
   └─ Recibe LoginResponse
   └─ Guarda token en localStorage del navegador
   └─ Guarda datos del usuario en localStorage
   └─ Actualiza estado de autenticación

8. Login.razor
   └─ Notifica a CustomAuthStateProvider
   └─ Navega a /Home

9. Home.razor
   └─ Verifica autenticación
   └─ Si está autenticado, muestra contenido
   └─ Si no, redirige a /login
```

### 2.2 Flujo de Registro

```
1. Usuario → Navegador
   └─ Accede a http://localhost:8098/register

2. Gateway.Blazor (Register.razor)
   └─ Usuario completa formulario
   └─ Validaciones del lado del cliente

3. Register.razor
   └─ Envía POST http://localhost:8096/api/auth/register

4. Gateway.Api (/api/auth/register endpoint)
   └─ Recibe RegisterRequest
   └─ Llama a AuthService.RegisterAsync()

5. AuthService (backend)
   └─ Verifica que el email no exista
   └─ Hashea la contraseña con BCrypt
   └─ Crea usuario en base de datos
   └─ Retorna UserDto

6. Gateway.Api
   └─ Retorna UserDto (HTTP 200) o error (HTTP 400)

7. Register.razor
   └─ Si éxito, muestra mensaje y redirige a /login
   └─ Si error, muestra mensaje de error
```

### 2.3 Cómo se Conectan los Proyectos

#### **Gateway.Blazor → Gateway.Api**
- **Método**: HTTP REST
- **Configuración**: 
  - Archivo: `src/Gateway.Blazor/appsettings.json`
  - Propiedad: `"ApiBaseUrl": "http://localhost:8096"`
  - También se configura en `Program.cs`:
    ```csharp
    builder.Services.AddScoped(sp => new HttpClient 
    { 
        BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8096") 
    });
    ```

#### **Gateway.Api → Base de Datos**
- **Método**: Entity Framework Core con SQLite
- **Configuración**:
  - Archivo: `src/Gateway.Api/appsettings.json`
  - Connection String: `"ConnectionStrings": { "DefaultConnection": "Data Source=multiagent.db" }`
  - Base de datos: `src/Gateway.Api/multiagent.db`

#### **Gateway.Api → Ollama (Opcional)**
- **Método**: HTTP REST
- **Configuración**:
  - Archivo: `src/Gateway.Api/appsettings.json`
  - Propiedad: `"OpenAI": { "BaseUrl": "http://localhost:11434/v1", ... }`
- **Solo necesario para chat/agentes**

---

## 3. PREPARACIÓN DEL ENTORNO

### 3.1 Requisitos Previos

1. **.NET SDK 9.0 o superior**
   - Verificar: `dotnet --version`
   - Si no está instalado: https://dotnet.microsoft.com/download

2. **Visual Studio 2022 o Visual Studio Code**
   - Visual Studio: Recomendado para desarrollo completo
   - VS Code: Funcional, pero menos integrado

3. **Docker Desktop** (Opcional - solo si usas Docker)
   - Verificar: `docker --version`
   - Si no está instalado: https://www.docker.com/products/docker-desktop

4. **SQLite** (Ya incluido en .NET, no requiere instalación adicional)

### 3.2 Verificar Estado del Proyecto

```powershell
# Navegar a la raíz del proyecto
cd E:\Proyectos\PoC_Analisis_Desarrollo

# Verificar que todos los proyectos compilen
dotnet build

# Si hay errores, restaurar dependencias
dotnet restore

# Limpiar build anterior (opcional)
dotnet clean
```

### 3.3 Verificar Configuración de Puertos

**Gateway.Api** debe estar en puerto **8096**:
```json
// src/Gateway.Api/Properties/launchSettings.json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:8096"
    }
  },
  "iisSettings": {
    "iisExpress": {
      "applicationUrl": "http://localhost:8096/"
    }
  }
}
```

**Gateway.Blazor** debe estar en puerto **8098**:
```json
// src/Gateway.Blazor/Properties/launchSettings.json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:8098"
    }
  },
  "iisSettings": {
    "iisExpress": {
      "applicationUrl": "http://localhost:8098"
    }
  }
}
```

**Gateway.Blazor** debe apuntar a **8096**:
```json
// src/Gateway.Blazor/appsettings.json
{
  "ApiBaseUrl": "http://localhost:8096"
}
```

### 3.4 Verificar Base de Datos

La base de datos SQLite se crea automáticamente cuando Gateway.Api inicia por primera vez.

**Ubicación**: `src/Gateway.Api/multiagent.db`

**Verificar que existe**:
```powershell
Test-Path "src/Gateway.Api/multiagent.db"
```

**Si no existe, se creará automáticamente** cuando Gateway.Api inicie.

---

## 4. EJECUCIÓN DEL PROYECTO

### 4.1 Opción 1: Ejecución Manual (Recomendada para Debugging)

Esta opción te da **control total** sobre cada proyecto y permite ver logs detallados.

#### **Paso 1: Iniciar Gateway.Api**

**Opción A: Desde Visual Studio**
1. Abre `MultiAgentSystem.sln` en Visual Studio
2. Establece `Gateway.Api` como proyecto de inicio
3. Selecciona el perfil **"http"** (no "IIS Express" ni "https")
4. Presiona F5 o clic en "Iniciar"

**Opción B: Desde Consola/PowerShell**
```powershell
# Navegar al proyecto
cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Api

# Ejecutar
dotnet run

# O especificar el perfil
dotnet run --launch-profile http
```

**Verificar que Gateway.Api está corriendo**:
- Abre un navegador en: `http://localhost:8096`
- Deberías ver:
  ```json
  {
    "message": "Multi-Agent Gateway API",
    "version": "v1",
    "documentation": "/swagger",
    "endpoints": { ... }
  }
  ```

- También puedes abrir Swagger: `http://localhost:8096/swagger`

**⚠️ IMPORTANTE**: Deja esta consola/ventana abierta. Gateway.Api debe seguir corriendo.

#### **Paso 2: Iniciar Gateway.Blazor**

**Abre una NUEVA terminal/consola** (Gateway.Api debe seguir corriendo en la anterior).

**Opción A: Desde Visual Studio (Proyecto Múltiple)**
1. En Visual Studio, clic derecho en la solución
2. "Properties" → "Startup Project"
3. Selecciona "Multiple startup projects"
4. Establece `Gateway.Api` y `Gateway.Blazor` como "Start"
5. Presiona F5

**Opción B: Desde Consola/PowerShell**
```powershell
# Abre una NUEVA terminal/consola
# Navegar al proyecto
cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Blazor

# Ejecutar
dotnet run

# O especificar el perfil
dotnet run --launch-profile http
```

**Verificar que Gateway.Blazor está corriendo**:
- Abre un navegador en: `http://localhost:8098`
- Deberías ver la página de inicio

### 4.2 Opción 2: Ejecución con Docker (Opcional)

**⚠️ ADVERTENCIA**: Docker requiere configuración adicional y no es necesario para debugging básico. Solo úsalo si ya estás familiarizado con Docker.

```powershell
# Navegar a la carpeta de infraestructura
cd E:\Proyectos\PoC_Analisis_Desarrollo\infra

# Construir e iniciar todos los servicios
docker-compose up -d

# Ver logs
docker-compose logs -f

# Detener servicios
docker-compose down
```

**Nota**: Docker usa puertos diferentes:
- Gateway.Api: `8094` (no 8096)
- Gateway.Blazor: `8093` (no 8098)

---

## 5. HERRAMIENTAS DE DEPURACIÓN

### 5.1 Navegador: Developer Tools (F12)

**Cómo abrir**: Presiona `F12` en cualquier navegador moderno (Chrome, Edge, Firefox)

#### **Pestaña "Console"**
- Muestra errores de JavaScript
- Muestra errores de Blazor
- Permite ejecutar comandos JavaScript

**Ejemplos de errores comunes**:
```
Failed to load resource: net::ERR_CONNECTION_REFUSED
→ Gateway.Api no está corriendo o está en el puerto incorrecto

401 (Unauthorized)
→ Token JWT expirado o inválido

500 (Internal Server Error)
→ Error en el servidor, revisa los logs de Gateway.Api
```

#### **Pestaña "Network"**
- Muestra todas las peticiones HTTP
- Permite ver:
  - **Request**: Qué se envía al servidor
  - **Response**: Qué responde el servidor
  - **Status Code**: 200 (éxito), 401 (no autorizado), 500 (error), etc.
  - **Timing**: Cuánto tarda cada petición

**Cómo usar**:
1. Abre Developer Tools (F12)
2. Pestaña "Network"
3. Recarga la página (F5)
4. Realiza una acción (ej: login)
5. Busca la petición a `/api/auth/login`
6. Haz clic en ella para ver detalles

#### **Pestaña "Application" (Chrome/Edge)**
- Permite ver y modificar `localStorage`
- Útil para verificar si el token se guardó correctamente

**Cómo verificar el token**:
1. Pestaña "Application"
2. En el menú izquierdo: "Storage" → "Local Storage" → `http://localhost:8098`
3. Deberías ver:
   - `token`: El token JWT
   - `user`: Datos del usuario en JSON
   - `loginTimestamp`: Timestamp del login

### 5.2 Visual Studio: Debugging

#### **Breakpoints**
- Coloca un breakpoint haciendo clic en el margen izquierdo del editor
- El código se detendrá cuando llegue a esa línea

**Dónde colocar breakpoints útiles**:

**En Gateway.Api**:
- `src/Gateway.Api/Program.cs` línea 199 (endpoint de login)
- `src/Gateway.Api/Services/AuthService.cs` línea 41 (método LoginAsync)

**En Gateway.Blazor**:
- `src/Gateway.Blazor/Services/AuthService.cs` línea 102 (método LoginAsync)
- `src/Gateway.Blazor/Pages/Login.razor` línea donde está `HandleLogin`

#### **Ventanas de Debug**
- **"Locals"**: Variables locales en el contexto actual
- **"Watch"**: Expresiones que quieres monitorear
- **"Call Stack"**: Cadena de llamadas de métodos
- **"Output"**: Logs del sistema

### 5.3 Logs del Servidor

#### **Gateway.Api - Logs en Consola**
Cuando ejecutas `dotnet run`, verás logs en la consola:
```
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/1.1 POST http://localhost:8096/api/auth/login
info: Gateway.Api.Services.AuthService[0]
      Login attempt - Email: user@example.com
```

**Niveles de log**:
- `info`: Información general
- `warn`: Advertencias (cosas que podrían ser problemas)
- `error`: Errores críticos

#### **Gateway.Blazor - Logs en Consola**
Similar a Gateway.Api, pero los logs aparecen en la consola donde ejecutaste `dotnet run`.

#### **Gateway.Blazor - Logs en el Navegador**
Abre Developer Tools (F12) → Pestaña "Console" para ver logs de Blazor.

### 5.4 Verificar Estado de los Servicios

#### **Verificar que Gateway.Api está corriendo**
```powershell
# Opción 1: Navegador
# Abre: http://localhost:8096

# Opción 2: PowerShell
Invoke-WebRequest -Uri "http://localhost:8096" -UseBasicParsing

# Opción 3: Ver qué procesos están usando el puerto 8096
Get-NetTCPConnection -LocalPort 8096 -ErrorAction SilentlyContinue | Select-Object -Property LocalAddress, LocalPort, State, OwningProcess
```

#### **Verificar que Gateway.Blazor está corriendo**
```powershell
# Opción 1: Navegador
# Abre: http://localhost:8098

# Opción 2: PowerShell
Invoke-WebRequest -Uri "http://localhost:8098" -UseBasicParsing
```

### 5.5 Herramientas de Prueba de API

#### **Swagger UI** (Recomendado)
- URL: `http://localhost:8096/swagger`
- Interfaz gráfica para probar endpoints
- Permite autenticarte y probar endpoints protegidos

**Cómo usar**:
1. Abre `http://localhost:8096/swagger`
2. Expande el endpoint que quieres probar (ej: `POST /api/auth/login`)
3. Haz clic en "Try it out"
4. Completa los datos
5. Haz clic en "Execute"
6. Verás la respuesta

#### **Postman o Insomnia** (Opcional)
- Herramientas externas para probar APIs
- Más potentes que Swagger, pero requieren configuración

#### **curl** (Línea de comandos)
```powershell
# Probar endpoint de login
curl -X POST "http://localhost:8096/api/auth/login" `
  -H "Content-Type: application/json" `
  -d '{\"email\":\"test@example.com\",\"password\":\"Password123!\"}'
```

---

## 6. VERIFICACIÓN Y TESTING

### 6.1 Checklist de Verificación Inicial

Antes de intentar hacer login, verifica:

- [ ] Gateway.Api está corriendo en `http://localhost:8096`
- [ ] Gateway.Blazor está corriendo en `http://localhost:8098`
- [ ] La base de datos `multiagent.db` existe
- [ ] No hay errores en las consolas de los proyectos
- [ ] Los puertos 8096 y 8098 no están siendo usados por otro proceso

### 6.2 Verificar que la API Responde

1. **Abre el navegador**: `http://localhost:8096`
2. **Deberías ver**:
   ```json
   {
     "message": "Multi-Agent Gateway API",
     "version": "v1",
     "documentation": "/swagger",
     "endpoints": { ... }
   }
   ```

3. **Si no ves esto**:
   - Gateway.Api no está corriendo
   - Está corriendo en un puerto diferente
   - Hay un error en el código

### 6.3 Verificar que Blazor Responde

1. **Abre el navegador**: `http://localhost:8098`
2. **Deberías ver**: La página de inicio con el botón "Iniciar Sesión"

3. **Si ves una página en blanco**:
   - Abre Developer Tools (F12)
   - Revisa la pestaña "Console" para ver errores
   - Revisa la pestaña "Network" para ver si hay peticiones fallidas

### 6.4 Probar Login desde Swagger

1. Abre `http://localhost:8096/swagger`
2. Busca el endpoint `POST /api/auth/login`
3. Expande y haz clic en "Try it out"
4. Completa:
   ```json
   {
     "email": "test@example.com",
     "password": "Test123!"
   }
   ```
5. Haz clic en "Execute"
6. **Si funciona**: Verás una respuesta 200 con un token
7. **Si no funciona**: Verás un error 401 (no autorizado) o 500 (error interno)

### 6.5 Probar Login desde la Interfaz Web

1. Abre `http://localhost:8098`
2. Haz clic en "Iniciar Sesión"
3. Abre Developer Tools (F12) → Pestaña "Network"
4. Ingresa credenciales y haz clic en "Iniciar Sesión"
5. **En la pestaña Network**:
   - Busca la petición a `/api/auth/login`
   - Verifica el Status Code (200 = éxito, 401 = no autorizado, etc.)
   - Haz clic en la petición para ver Request y Response

### 6.6 Verificar Base de Datos

#### **Ver usuarios en la base de datos**

**Opción 1: SQLite Command Line**
```powershell
# Si tienes SQLite CLI instalado
sqlite3 src/Gateway.Api/multiagent.db "SELECT Id, Email, Name, Role, IsActive FROM Users;"
```

**Opción 2: Usar una herramienta gráfica**
- **DB Browser for SQLite**: https://sqlitebrowser.org/
- Abre el archivo: `src/Gateway.Api/multiagent.db`
- Navega a la tabla "Users"

**Opción 3: Desde Swagger**
- Si estás autenticado como SuperUsuario
- Endpoint: `GET /api/admin/users`

---

## 7. TROUBLESHOOTING COMPLETO

### 7.1 Problema: "No puedo ver nada en el navegador" (Página en blanco)

#### **Diagnóstico**:

1. **Abre Developer Tools (F12)**
   - Pestaña "Console": ¿Hay errores en rojo?
   - Pestaña "Network": ¿Hay peticiones fallidas?

2. **Verifica que Gateway.Blazor está corriendo**:
   ```powershell
   Invoke-WebRequest -Uri "http://localhost:8098" -UseBasicParsing
   ```

3. **Revisa la consola donde ejecutaste Gateway.Blazor**:
   - ¿Hay errores?
   - ¿Dice "Now listening on: http://localhost:8098"?

#### **Soluciones comunes**:

**Error: "ERR_CONNECTION_REFUSED"**
- Gateway.Blazor no está corriendo
- Está corriendo en un puerto diferente
- **Solución**: Inicia Gateway.Blazor correctamente

**Error: "ERR_SSL_PROTOCOL_ERROR"**
- Estás intentando usar HTTPS cuando el proyecto está configurado para HTTP
- **Solución**: Asegúrate de usar `http://localhost:8098` (no `https://`)

**Error en Console: "Failed to load resource"**
- Gateway.Blazor no puede cargar archivos estáticos
- **Solución**: Verifica que la carpeta `wwwroot` existe y tiene archivos

**Error en Console: JavaScript errors**
- Hay errores en el código JavaScript o Blazor
- **Solución**: Revisa los logs detallados en Developer Tools

### 7.2 Problema: "El login no funciona" (No navega a /Home)

#### **Diagnóstico paso a paso**:

**Paso 1: Verificar que Gateway.Api está corriendo**
```powershell
# Abre en navegador
http://localhost:8096

# O desde PowerShell
Invoke-WebRequest -Uri "http://localhost:8096" -UseBasicParsing
```

**Paso 2: Verificar la petición de login en Network**

1. Abre Developer Tools (F12) → Pestaña "Network"
2. Intenta hacer login
3. Busca la petición a `/api/auth/login`
4. Haz clic en ella para ver detalles

**¿Qué Status Code ves?**
- **200 (OK)**: El login fue exitoso en el backend
  - **Problema**: El frontend no está procesando la respuesta correctamente
  - **Revisa**: `src/Gateway.Blazor/Services/AuthService.cs` línea 102-164
  - **Revisa**: `src/Gateway.Blazor/Pages/Login.razor` método `HandleLogin`

- **401 (Unauthorized)**: Credenciales incorrectas
  - **Verifica**: ¿El usuario existe en la base de datos?
  - **Verifica**: ¿La contraseña es correcta?
  - **Revisa logs**: Consola de Gateway.Api para ver mensajes de error

- **500 (Internal Server Error)**: Error en el servidor
  - **Revisa logs**: Consola de Gateway.Api
  - **Busca**: Mensajes de error o excepciones

- **0 o ERR_CONNECTION_REFUSED**: Gateway.Api no está corriendo
  - **Solución**: Inicia Gateway.Api

**Paso 3: Verificar el token en localStorage**

1. Developer Tools (F12) → Pestaña "Application"
2. Local Storage → `http://localhost:8098`
3. ¿Existe `token`?
   - **Sí**: El token se guardó, el problema está en la navegación
   - **No**: El token no se guardó, revisa `AuthService.cs`

**Paso 4: Verificar logs del servidor**

En la consola de Gateway.Api, busca:
```
info: Gateway.Api.Services.AuthService[0]
      Login attempt - Email: ...
```

Si hay un error, aparecerá como:
```
error: Gateway.Api.Services.AuthService[0]
      ...
```

#### **Soluciones comunes**:

**"Token vacío en la respuesta"**
- El backend no está generando el token correctamente
- **Revisa**: `src/Gateway.Api/Services/AuthService.cs` método `GenerateJwtToken`

**"Respuesta de login con formato inválido"**
- El backend está retornando un formato diferente al esperado
- **Revisa**: `src/Gateway.Api/Program.cs` endpoint `/api/auth/login`
- **Revisa**: `src/Gateway.Blazor/Services/AuthService.cs` deserialización

**"El usuario o la contraseña no coinciden"**
- Verifica que el usuario existe en la base de datos
- Verifica que la contraseña es correcta
- **Para SuperUsuario**: Verifica que el hash en `appsettings.json` es correcto

### 7.3 Problema: "El registro no funciona"

#### **Diagnóstico**:

1. **Abre Developer Tools (F12) → Pestaña "Network"**
2. Intenta registrarte
3. Busca la petición a `/api/auth/register`
4. Verifica el Status Code

**Status Code 400 (Bad Request)**:
- "El email ya existe en la base de datos"
  - **Solución**: Usa otro email o elimina el usuario existente
- "La contraseña no cumple con los requisitos"
  - **Verifica**: Longitud entre 8-32 caracteres
  - **Verifica**: Tiene mayúsculas, números y caracteres especiales (`: ; _ - # @`)

**Status Code 500 (Internal Server Error)**:
- Error en el servidor
- **Revisa logs**: Consola de Gateway.Api

**Status Code 0 o ERR_CONNECTION_REFUSED**:
- Gateway.Api no está corriendo
- **Solución**: Inicia Gateway.Api

### 7.4 Problema: "Gateway.Api no inicia"

#### **Errores comunes**:

**"Port 8096 is already in use"**
- Otro proceso está usando el puerto 8096
- **Solución**:
  ```powershell
  # Ver qué proceso está usando el puerto
  Get-NetTCPConnection -LocalPort 8096 -ErrorAction SilentlyContinue | Select-Object -Property OwningProcess
  
  # Matar el proceso (reemplaza PID con el número del proceso)
  Stop-Process -Id <PID> -Force
  ```

**"DirectoryNotFoundException: wwwroot"**
- La carpeta wwwroot no existe
- **Solución**: Se puede ignorar si Gateway.Api es solo una API (no sirve archivos estáticos)

**"Cannot open database file"**
- Error al acceder a la base de datos SQLite
- **Verifica**: Permisos de archivo en `src/Gateway.Api/multiagent.db`
- **Solución**: Elimina el archivo y deja que se recree

### 7.5 Problema: "Gateway.Blazor no puede conectarse a Gateway.Api"

#### **Diagnóstico**:

1. **Verifica la configuración**:
   - `src/Gateway.Blazor/appsettings.json`: `"ApiBaseUrl": "http://localhost:8096"`
   - `src/Gateway.Api/Properties/launchSettings.json`: Puerto 8096

2. **Verifica que Gateway.Api está corriendo**:
   ```powershell
   Invoke-WebRequest -Uri "http://localhost:8096" -UseBasicParsing
   ```

3. **Abre Developer Tools (F12) → Pestaña "Network"**
   - ¿Las peticiones a `/api/*` fallan?
   - ¿Aparecen como "pending" indefinidamente?

#### **Soluciones**:

**Error CORS**:
- Gateway.Api debe tener CORS habilitado
- **Verifica**: `src/Gateway.Api/Program.cs` tiene `app.UseCors()`

**Error de conexión**:
- Gateway.Api no está corriendo
- Gateway.Api está en un puerto diferente
- **Solución**: Inicia Gateway.Api en el puerto correcto

### 7.6 Problema: "Los cambios no se reflejan"

#### **Soluciones**:

**Hot Reload no funciona**:
- Reinicia el proyecto manualmente (Ctrl+C y luego `dotnet run` nuevamente)

**Cache del navegador**:
- Presiona Ctrl+Shift+R (recarga forzada)
- O limpia el cache del navegador

**Build desactualizado**:
```powershell
# Limpiar y recompilar
dotnet clean
dotnet build
dotnet run
```

### 7.7 Reinicio Completo (Empezar desde Cero)

Si nada funciona, sigue estos pasos para reiniciar todo desde cero:

```powershell
# 1. Detener todos los procesos de .NET
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Verificar que los puertos están libres
Get-NetTCPConnection -LocalPort 8096 -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 8098 -ErrorAction SilentlyContinue
# Si hay procesos, mátalos

# 3. Limpiar builds anteriores
cd E:\Proyectos\PoC_Analisis_Desarrollo
dotnet clean

# 4. Restaurar dependencias
dotnet restore

# 5. Recompilar
dotnet build

# 6. Iniciar Gateway.Api (en una terminal)
cd src\Gateway.Api
dotnet run

# 7. En OTRA terminal, iniciar Gateway.Blazor
cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Blazor
dotnet run
```

---

## 8. GLOSARIO DE TÉRMINOS

### **Arquitectura**

- **Cliente-Servidor**: Arquitectura donde el cliente (navegador) hace peticiones al servidor (API)
- **REST API**: Interfaz de programación que usa HTTP (GET, POST, PUT, DELETE)
- **JWT (JSON Web Token)**: Token de autenticación que contiene información del usuario
- **Blazor Server-Side**: Framework que ejecuta el código C# en el servidor y actualiza el DOM mediante SignalR

### **Componentes**

- **Frontend**: La parte del sistema que el usuario ve y con la que interactúa (Gateway.Blazor)
- **Backend**: La parte del sistema que procesa la lógica de negocio (Gateway.Api)
- **Base de datos**: Almacenamiento persistente de datos (SQLite en este caso)

### **Procesos**

- **Endpoint**: Un punto de acceso a la API (ej: `/api/auth/login`)
- **Request**: Petición que envía el cliente al servidor
- **Response**: Respuesta que envía el servidor al cliente
- **Status Code**: Código numérico que indica el resultado de una petición HTTP (200 = éxito, 401 = no autorizado, 500 = error)

### **Herramientas**

- **Developer Tools (F12)**: Herramientas del navegador para depurar
- **Breakpoint**: Punto donde el código se detiene durante el debugging
- **Console**: Terminal donde se ejecutan comandos o se muestran logs
- **Swagger**: Interfaz gráfica para probar APIs

### **Errores Comunes**

- **ERR_CONNECTION_REFUSED**: El servidor no está corriendo o el puerto está bloqueado
- **ERR_SSL_PROTOCOL_ERROR**: Intento de usar HTTPS cuando el servidor solo acepta HTTP
- **401 Unauthorized**: No estás autenticado o las credenciales son incorrectas
- **500 Internal Server Error**: Error en el código del servidor

---

## 9. FLUJO DE TRABAJO RECOMENDADO

### Para Desarrollo Diario:

1. **Iniciar Gateway.Api**:
   ```powershell
   cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Api
   dotnet run
   ```
   - Verificar: `http://localhost:8096` responde
   - Dejar esta terminal abierta

2. **Iniciar Gateway.Blazor** (en otra terminal):
   ```powershell
   cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Blazor
   dotnet run
   ```
   - Verificar: `http://localhost:8098` muestra la página de inicio
   - Dejar esta terminal abierta

3. **Desarrollo**:
   - Hacer cambios en el código
   - Guardar archivos
   - El proyecto puede recargar automáticamente (Hot Reload)
   - Si no recarga, presiona Ctrl+C en la terminal y vuelve a ejecutar `dotnet run`

4. **Debugging**:
   - Coloca breakpoints en Visual Studio
   - Abre Developer Tools (F12) en el navegador
   - Revisa logs en las consolas

5. **Al terminar**:
   - Presiona Ctrl+C en ambas terminales para detener los proyectos

### Para Troubleshooting:

1. **Verificar estado de servicios** (puertos, procesos)
2. **Revisar logs** (consolas, Developer Tools)
3. **Verificar configuración** (puertos, URLs, appsettings.json)
4. **Probar endpoints directamente** (Swagger)
5. **Reiniciar todo** si es necesario

---

## 10. PRÓXIMOS PASOS

Ahora que tienes esta guía:

1. **Sigue el flujo de trabajo recomendado** para iniciar los proyectos
2. **Usa las herramientas de debugging** cuando algo no funcione
3. **Revisa el troubleshooting** para problemas comunes
4. **Experimenta** con las herramientas (Swagger, Developer Tools, etc.)

**Recuerda**: La depuración es un proceso iterativo. No te desanimes si algo no funciona de inmediato. Sigue los pasos sistemáticamente y revisa los logs.

---

**¿Necesitas ayuda?** Revisa:
- Logs en las consolas de los proyectos
- Developer Tools (F12) en el navegador
- Esta guía de troubleshooting
- Los archivos de configuración (appsettings.json, launchSettings.json)

**¡Éxito con tu debugging!** 🚀

