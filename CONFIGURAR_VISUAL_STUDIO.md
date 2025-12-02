# Configurar Visual Studio para Ejecutar Múltiples Proyectos

## Contexto

El proyecto requiere ejecutar **dos proyectos simultáneamente**:

1. **Gateway.Api** - Backend API REST (puerto **8096**)
2. **Gateway.Blazor** - Frontend Blazor Server (puerto **8098**)

Gateway.Blazor necesita comunicarse con Gateway.Api para funcionar correctamente.

## Configuración en Visual Studio

### Método 1: Configurar Múltiples Proyectos de Inicio (Recomendado)

1. En Visual Studio, haz clic derecho sobre la **Solución** (nodo raíz del Solution Explorer)
2. Selecciona **Properties** (Propiedades)
3. En el panel izquierdo, selecciona **Startup Project**
4. Selecciona la opción **Multiple startup projects**
5. En la lista de proyectos:
   - Busca **Gateway.Api** y cambia la acción a **Start**
   - Busca **Gateway.Blazor** y cambia la acción a **Start**
6. Asegúrate de que el orden sea:
   - Gateway.Api (inicia primero)
   - Gateway.Blazor (inicia después)
7. Haz clic en **OK** o **Apply**

### Método 2: Configurar Proyectos de Inicio Rápidamente

1. En Visual Studio, haz clic derecho sobre la **Solución**
2. Selecciona **Set Startup Projects...**
3. Selecciona **Multiple startup projects**
4. Establece ambos proyectos en **Start**
5. Ajusta el orden si es necesario (Gateway.Api primero)

### Método 3: Ejecutar Manualmente

Si prefieres ejecutar manualmente:

1. Ejecuta primero **Gateway.Api**:
   - Click derecho en `Gateway.Api` → **Set as Startup Project**
   - Presiona **F5** o **Ctrl+F5**

2. Luego ejecuta **Gateway.Blazor** en otra instancia:
   - Click derecho en `Gateway.Blazor` → **Set as Startup Project**
   - Presiona **F5** o **Ctrl+F5**

**Nota:** Este método requiere que ejecutes ambos proyectos en procesos separados.

## Verificación

Una vez configurado:

1. **Gateway.Api** debe ejecutarse en: `http://localhost:8096`
   - Puedes verificar accediendo a: `http://localhost:8096/swagger`
   - El endpoint raíz está disponible en: `http://localhost:8096/`

2. **Gateway.Blazor** debe ejecutarse en: `http://localhost:8098` (HTTP)
   - **IMPORTANTE:** Asegúrate de seleccionar el perfil **"http"** en el dropdown de perfiles de Visual Studio (no "https" ni "IIS Express")
   - Visual Studio debería abrir automáticamente el navegador en `http://localhost:8098`
   - Si ves un error SSL (`ERR_SSL_PROTOCOL_ERROR`), significa que estás usando el perfil incorrecto

3. Verifica que Gateway.Blazor pueda comunicarse con Gateway.Api:
   - Abre las herramientas de desarrollador del navegador (F12)
   - Ve a la pestaña **Network**
   - Intenta hacer login
   - Deberías ver peticiones a `http://localhost:8096/api/auth/login`

## Puertos Configurados

### Gateway.Api
- **Puerto HTTP:** 8096
- **Archivo de configuración:** `src/Gateway.Api/Properties/launchSettings.json`
- **URL de Swagger:** `http://localhost:8096/swagger`

### Gateway.Blazor
- **Puerto HTTP:** 8098
- **Puerto HTTPS:** 7167 (perfil https)
- **Archivo de configuración:** `src/Gateway.Blazor/Properties/launchSettings.json`
- **URL Base de API:** `http://localhost:8096` (configurado en `appsettings.json` y `Program.cs`)
  - Gateway.Blazor se ejecuta en el puerto 8098 pero se conecta a la API en 8096

## Solución de Problemas

### Error: "Unable to connect to API"
- Verifica que Gateway.Api esté ejecutándose en el puerto 8096
- Verifica que la URL en `Gateway.Blazor/appsettings.json` sea `http://localhost:8096`

### Error: "Port already in use"
- Cierra otros procesos que estén usando los puertos 8096 o 8098
- O cambia los puertos en los archivos `launchSettings.json` correspondientes

### Error: "ERR_SSL_PROTOCOL_ERROR" o "Este sitio no puede proporcionar una conexión segura"
- **Causa:** Visual Studio está usando el perfil "https" o "IIS Express" en lugar del perfil "http"
- **Solución:**
  1. En Visual Studio, en la barra de herramientas, busca el dropdown de perfiles (junto al botón de ejecutar)
  2. Selecciona el perfil **"http"** para Gateway.Blazor (NO "https" ni "IIS Express")
  3. Ejecuta nuevamente el proyecto
  4. Deberías acceder a `http://localhost:8098` (HTTP, no HTTPS)

### Los proyectos no inician simultáneamente
- Verifica que hayas configurado "Multiple startup projects" correctamente
- Asegúrate de que ambos proyectos tengan la acción "Start" configurada

