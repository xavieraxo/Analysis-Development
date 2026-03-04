# ✅ CHECKLIST DE DEBUGGING - Paso a Paso

Usa este checklist cuando algo no funciona. Sigue los pasos en orden y marca cada uno cuando lo completes.

---

## 🔍 DIAGNÓSTICO INICIAL

### Paso 1: Verificar Estado de los Servicios

#### 1.1 ¿Gateway.Api está corriendo?
```
□ Abrir: http://localhost:8096 en el navegador
□ ¿Ves un JSON con "message": "Multi-Agent Gateway API"?
   □ SÍ → Continúa al paso 1.2
   □ NO → Ve a "🔧 SOLUCIÓN: Gateway.Api no responde"
```

#### 1.2 ¿Gateway.Blazor está corriendo?
```
□ Abrir: http://localhost:8098 en el navegador
□ ¿Ves la página de inicio con el botón "Iniciar Sesión"?
   □ SÍ → Continúa al paso 1.3
   □ NO → Ve a "🔧 SOLUCIÓN: Gateway.Blazor no responde"
```

#### 1.3 Verificar Puertos
```powershell
# Ejecuta estos comandos en PowerShell
□ Get-NetTCPConnection -LocalPort 8096 -ErrorAction SilentlyContinue
   □ ¿Muestra algo? → Puerto 8096 está en uso (BIEN si Gateway.Api está corriendo)
   □ ¿No muestra nada? → Puerto 8096 está libre (MAL, Gateway.Api no está corriendo)

□ Get-NetTCPConnection -LocalPort 8098 -ErrorAction SilentlyContinue
   □ ¿Muestra algo? → Puerto 8098 está en uso (BIEN si Gateway.Blazor está corriendo)
   □ ¿No muestra nada? → Puerto 8098 está libre (MAL, Gateway.Blazor no está corriendo)
```

---

## 🐛 DEBUGGING DE PROBLEMAS ESPECÍFICOS

### 🔧 SOLUCIÓN: Gateway.Api no responde

```
□ Verificar que Gateway.Api está ejecutándose
   □ Abre la terminal donde ejecutaste: cd src\Gateway.Api && dotnet run
   □ ¿Hay errores en rojo en la consola?
      □ SÍ → Copia el error y busca solución en "GUIA_COMPLETA_DEBUGGING.md"
      □ NO → Continúa

□ Verificar que el puerto 8096 está configurado correctamente
   □ Abre: src\Gateway.Api\Properties\launchSettings.json
   □ Verifica: "applicationUrl": "http://localhost:8096"
      □ ¿Es correcto? → Continúa
      □ ¿No es correcto? → Corrígelo y reinicia Gateway.Api

□ Verificar que ningún otro proceso usa el puerto 8096
   □ Ejecuta: Get-NetTCPConnection -LocalPort 8096
   □ ¿Hay otro proceso usando el puerto?
      □ SÍ → Mátalo: Stop-Process -Id <PID> -Force
      □ NO → Continúa

□ Reiniciar Gateway.Api
   □ Presiona Ctrl+C en la terminal de Gateway.Api
   □ Ejecuta: cd src\Gateway.Api && dotnet run
   □ Espera a que muestre: "Now listening on: http://localhost:8096"
```

### 🔧 SOLUCIÓN: Gateway.Blazor no responde

```
□ Verificar que Gateway.Blazor está ejecutándose
   □ Abre la terminal donde ejecutaste: cd src\Gateway.Blazor && dotnet run
   □ ¿Hay errores en rojo en la consola?
      □ SÍ → Copia el error y busca solución en "GUIA_COMPLETA_DEBUGGING.md"
      □ NO → Continúa

□ Verificar que el puerto 8098 está configurado correctamente
   □ Abre: src\Gateway.Blazor\Properties\launchSettings.json
   □ Verifica: "applicationUrl": "http://localhost:8098"
      □ ¿Es correcto? → Continúa
      □ ¿No es correcto? → Corrígelo y reinicia Gateway.Blazor

□ Verificar que ningún otro proceso usa el puerto 8098
   □ Ejecuta: Get-NetTCPConnection -LocalPort 8098
   □ ¿Hay otro proceso usando el puerto?
      □ SÍ → Mátalo: Stop-Process -Id <PID> -Force
      □ NO → Continúa

□ Abrir Developer Tools (F12) en el navegador
   □ Pestaña "Console": ¿Hay errores en rojo?
      □ SÍ → Copia el error y busca solución
      □ NO → Continúa
   □ Pestaña "Network": ¿Hay peticiones fallidas?
      □ SÍ → Haz clic en ellas para ver detalles
      □ NO → Continúa

□ Reiniciar Gateway.Blazor
   □ Presiona Ctrl+C en la terminal de Gateway.Blazor
   □ Ejecuta: cd src\Gateway.Blazor && dotnet run
   □ Espera a que muestre: "Now listening on: http://localhost:8098"
```

### 🔧 SOLUCIÓN: El Login no funciona

```
□ PASO 1: Verificar que Gateway.Api está corriendo
   □ Abre: http://localhost:8096
   □ ¿Responde con JSON?
      □ NO → Ve a "🔧 SOLUCIÓN: Gateway.Api no responde"
      □ SÍ → Continúa

□ PASO 2: Probar Login desde Swagger
   □ Abre: http://localhost:8096/swagger
   □ Busca: POST /api/auth/login
   □ Haz clic en "Try it out"
   □ Completa:
      {
        "email": "test@example.com",
        "password": "Test123!"
      }
   □ Haz clic en "Execute"
   □ ¿Qué Status Code ves?
      □ 200 (OK) → El login funciona en el backend, el problema está en el frontend
         → Continúa al PASO 3
      □ 401 (Unauthorized) → Las credenciales son incorrectas o el usuario no existe
         → Ve a "🔧 SOLUCIÓN: Usuario no existe o credenciales incorrectas"
      □ 500 (Internal Server Error) → Error en el servidor
         → Ve a "🔧 SOLUCIÓN: Error 500 en el servidor"
      □ 0 o ERR_CONNECTION_REFUSED → Gateway.Api no está corriendo
         → Ve a "🔧 SOLUCIÓN: Gateway.Api no responde"

□ PASO 3: Probar Login desde la Web
   □ Abre: http://localhost:8098/login
   □ Abre Developer Tools (F12)
   □ Pestaña "Network" → Activa "Preserve log"
   □ Ingresa credenciales y haz clic en "Iniciar Sesión"
   □ Busca la petición a: /api/auth/login
   □ Haz clic en ella para ver detalles
   □ ¿Qué Status Code ves?
      □ 200 (OK) → La petición fue exitosa
         → Verifica si navega a /Home
         → Si no navega, ve a "🔧 SOLUCIÓN: No navega después del login"
      □ 401 (Unauthorized) → Credenciales incorrectas
         → Verifica que el usuario existe
      □ 500 (Internal Server Error) → Error en el servidor
         → Ve a "🔧 SOLUCIÓN: Error 500 en el servidor"
      □ 0 o ERR_CONNECTION_REFUSED → Gateway.Api no está corriendo
         → Ve a "🔧 SOLUCIÓN: Gateway.Api no responde"

□ PASO 4: Verificar Token en localStorage
   □ Developer Tools (F12) → Pestaña "Application"
   □ Local Storage → http://localhost:8098
   □ ¿Existe "token"?
      □ SÍ → El token se guardó correctamente
         → Si no navega, ve a "🔧 SOLUCIÓN: No navega después del login"
      □ NO → El token no se guardó
         → Revisa los errores en Console
         → Revisa src\Gateway.Blazor\Services\AuthService.cs
```

### 🔧 SOLUCIÓN: No navega después del login

```
□ Verificar Token en localStorage
   □ Developer Tools (F12) → Pestaña "Application"
   □ Local Storage → http://localhost:8098
   □ ¿Existe "token" y tiene un valor?
      □ NO → El token no se guardó, el problema está en AuthService
      □ SÍ → Continúa

□ Verificar Errores en Console
   □ Developer Tools (F12) → Pestaña "Console"
   □ ¿Hay errores en rojo?
      □ SÍ → Copia el error y revisa:
         → Si es "InvalidOperationException" relacionado con role
            → Revisa LOGIN_DIAGNOSIS.md
         → Si es otro error, busca solución en GUIA_COMPLETA_DEBUGGING.md

□ Verificar que CustomAuthStateProvider se actualiza
   □ Revisa: src\Gateway.Blazor\Pages\Login.razor
   □ Verifica que llama a: customProvider.NotifyAuthenticationStateChanged()
   □ Verifica que espera a que el estado se actualice antes de navegar

□ Verificar que Home.razor permite el acceso
   □ Revisa: src\Gateway.Blazor\Pages\Home.razor
   □ Verifica que verifica autenticación correctamente
   □ Verifica que no redirige inmediatamente a /login
```

### 🔧 SOLUCIÓN: Usuario no existe o credenciales incorrectas

```
□ Verificar Usuario en la Base de Datos
   □ Opción 1: Desde Swagger (si estás autenticado como SuperUsuario)
      → GET /api/admin/users
   □ Opción 2: Desde PostgreSQL (psql)
      → psql -h localhost -p 5433 -U appuser -d multiagent -c "SELECT \"Email\", \"Name\", \"Role\" FROM \"Users\";"
   □ Opción 3: Desde pgAdmin
      → Conecta a localhost:5433, DB multiagent
      → Ejecuta la consulta anterior

□ Crear Usuario si no existe
   □ Opción 1: Registro público
      → Abre: http://localhost:8098/register
      → Completa el formulario
      → Haz clic en "Registrarse"
   □ Opción 2: Desde Swagger
      → POST /api/auth/register
```

### 🔧 SOLUCIÓN: Error 500 en el servidor

```
□ Revisar Logs de Gateway.Api
   □ Abre la terminal donde ejecutaste Gateway.Api
   □ Busca líneas que comienzan con: "error:" o "fail:"
   □ Copia el error completo

□ Errores Comunes:
   □ "DirectoryNotFoundException: wwwroot"
      → Ignorar si Gateway.Api es solo una API
   □ "No se puede conectar a PostgreSQL"
      → Verifica que el contenedor esté activo
      → Verifica el puerto `5433` y la cadena de conexión
   □ "NullReferenceException"
      → Revisa el código en el punto donde falla
   □ Otro error
      → Busca en GUIA_COMPLETA_DEBUGGING.md
```

### 🔧 SOLUCIÓN: Error de conexión entre Blazor y Api

```
□ Verificar Configuración de URL
   □ Abre: src/Gateway.Blazor/appsettings.json
   □ Verifica: "ApiBaseUrl": "http://localhost:8096"
      □ ¿Es correcto? → Continúa
      □ ¿No es correcto? → Corrígelo y reinicia Gateway.Blazor

□ Verificar que Gateway.Api está en el puerto correcto
   □ Abre: src/Gateway.Api/Properties/launchSettings.json
   □ Verifica: "applicationUrl": "http://localhost:8096"
      □ ¿Es correcto? → Continúa
      □ ¿No es correcto? → Corrígelo y reinicia Gateway.Api

□ Verificar CORS
   □ Abre: src/Gateway.Api/Program.cs
   □ Busca: app.UseCors()
   □ ¿Existe? → Continúa
   □ ¿No existe? → Agrega:
      builder.Services.AddCors(options =>
      {
          options.AddDefaultPolicy(policy =>
          {
              policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
          });
      });
      // ...
      app.UseCors();

□ Probar conexión directamente
   □ Abre: http://localhost:8096 en el navegador
   □ ¿Responde? → Gateway.Api está corriendo
   □ ¿No responde? → Ve a "🔧 SOLUCIÓN: Gateway.Api no responde"
```

---

## 🔄 REINICIO COMPLETO

Si nada funciona, reinicia todo desde cero:

```
□ PASO 1: Detener todos los procesos
   □ Presiona Ctrl+C en todas las terminales donde ejecutaste dotnet run
   □ Ejecuta:
      Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

□ PASO 2: Verificar Puertos Libres
   □ Ejecuta:
      Get-NetTCPConnection -LocalPort 8096 -ErrorAction SilentlyContinue
      Get-NetTCPConnection -LocalPort 8098 -ErrorAction SilentlyContinue
   □ Si hay procesos, mátalos:
      Stop-Process -Id <PID> -Force

□ PASO 3: Limpiar Builds Anteriores
   □ cd E:\Proyectos\PoC_Analisis_Desarrollo
   □ dotnet clean
   □ dotnet restore
   □ dotnet build

□ PASO 4: Iniciar Gateway.Api (Terminal 1)
   □ cd src\Gateway.Api
   □ dotnet run
   □ Espera a que muestre: "Now listening on: http://localhost:8096"
   □ Verifica: http://localhost:8096 responde

□ PASO 5: Iniciar Gateway.Blazor (Terminal 2 - Nueva)
   □ cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Blazor
   □ dotnet run
   □ Espera a que muestre: "Now listening on: http://localhost:8098"
   □ Verifica: http://localhost:8098 responde

□ PASO 6: Probar de Nuevo
   □ Vuelve a intentar la acción que falló
   □ Si sigue fallando, revisa los pasos de diagnóstico específicos arriba
```

---

## 📝 NOTAS IMPORTANTES

### Al Debugging:

1. **Siempre verifica los logs** en las consolas de los proyectos
2. **Siempre abre Developer Tools (F12)** en el navegador
3. **Revisa la pestaña Network** para ver peticiones HTTP
4. **Revisa la pestaña Console** para ver errores de JavaScript/Blazor
5. **Copia los mensajes de error completos** para buscar soluciones

### Comandos Útiles:

```powershell
# Ver qué procesos usan los puertos
Get-NetTCPConnection -LocalPort 8096 -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 8098 -ErrorAction SilentlyContinue

# Matar un proceso por PID
Stop-Process -Id <PID> -Force

# Matar todos los procesos de dotnet
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

# Limpiar y recompilar
dotnet clean
dotnet restore
dotnet build
```

### Archivos Clave para Revisar:

- `src/Gateway.Api/Properties/launchSettings.json` → Puerto de la API
- `src/Gateway.Blazor/Properties/launchSettings.json` → Puerto de Blazor
- `src/Gateway.Blazor/appsettings.json` → URL de la API
- `src/Gateway.Api/appsettings.json` → Configuración de la base de datos

---

**¿Seguiste todos los pasos y nada funciona?** 

1. Revisa `GUIA_COMPLETA_DEBUGGING.md` para información detallada
2. Copia los mensajes de error completos
3. Revisa los logs en las consolas
4. Verifica que todos los archivos de configuración son correctos

**¡No te rindas!** La depuración es un proceso sistemático. Sigue los pasos uno por uno. 🚀

