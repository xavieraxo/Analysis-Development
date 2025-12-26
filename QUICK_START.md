# 🚀 QUICK START: Ejecutar el Proyecto en 5 Minutos

## ⚡ Inicio Rápido (Paso a Paso)

### Paso 1: Verificar Requisitos

```powershell
# Verificar .NET SDK
dotnet --version
# Debe mostrar: 9.0.x o superior

# Navegar al proyecto
cd E:\Proyectos\PoC_Analisis_Desarrollo
```

### Paso 2: Compilar el Proyecto

```powershell
# Limpiar y compilar
dotnet clean
dotnet restore
dotnet build
```

**Si hay errores**, revisa los mensajes y corrígelos antes de continuar.

### Paso 3: Iniciar Gateway.Api (Terminal 1)

```powershell
cd src\Gateway.Api
dotnet run
```

**✅ Verificar**: Abre `http://localhost:8096` en el navegador.

**Deberías ver**:
```json
{
  "message": "Multi-Agent Gateway API",
  "version": "v1",
  ...
}
```

**⚠️ IMPORTANTE**: Deja esta terminal abierta. Gateway.Api debe seguir corriendo.

### Paso 4: Iniciar Gateway.Blazor (Terminal 2 - Nueva Terminal)

```powershell
# Abre una NUEVA terminal/consola de PowerShell
cd E:\Proyectos\PoC_Analisis_Desarrollo\src\Gateway.Blazor
dotnet run
```

**✅ Verificar**: Abre `http://localhost:8098` en el navegador.

**Deberías ver**: La página de inicio con el botón "Iniciar Sesión".

**⚠️ IMPORTANTE**: Deja esta terminal abierta también.

---

## 🧪 Verificar que Todo Funciona

### 1. Verificar API

Abre en el navegador: `http://localhost:8096/swagger`

Deberías ver la documentación de la API.

### 2. Verificar Frontend

Abre en el navegador: `http://localhost:8098`

Deberías ver la página de inicio.

### 3. Probar Login desde Swagger

1. En Swagger (`http://localhost:8096/swagger`)
2. Busca `POST /api/auth/login`
3. Haz clic en "Try it out"
4. Completa:
   ```json
   {
     "email": "test@example.com",
     "password": "Test123!"
   }
   ```
5. Haz clic en "Execute"
6. **Si funciona**: Verás un token JWT en la respuesta
7. **Si no funciona**: Verás un error 401 (el usuario no existe)

### 4. Probar Login desde la Web

1. Abre `http://localhost:8098`
2. Haz clic en "Iniciar Sesión"
3. Ingresa credenciales (debes crear un usuario primero mediante registro)
4. Haz clic en "Iniciar Sesión"

---

## 🛠️ Si Algo No Funciona

### Problema: "No puedo ver la página"

1. **Abre Developer Tools (F12)** en el navegador
2. Revisa la pestaña **"Console"** para ver errores
3. Revisa la pestaña **"Network"** para ver peticiones fallidas

### Problema: "Gateway.Api no inicia"

```powershell
# Ver si el puerto 8096 está ocupado
Get-NetTCPConnection -LocalPort 8096 -ErrorAction SilentlyContinue

# Si hay un proceso, mátalo y vuelve a intentar
```

### Problema: "El login no funciona"

1. **Verifica que Gateway.Api está corriendo**:
   - Abre `http://localhost:8096` en el navegador
   - Debe mostrar un JSON con información de la API

2. **Abre Developer Tools (F12)** → Pestaña **"Network"**
3. Intenta hacer login
4. Busca la petición a `/api/auth/login`
5. Haz clic en ella para ver:
   - **Status Code**: 200 = éxito, 401 = no autorizado, 500 = error
   - **Response**: Qué responde el servidor

### Problema: "Error de conexión"

**"ERR_CONNECTION_REFUSED"**:
- Gateway.Api no está corriendo
- Inicia Gateway.Api (Paso 3)

**"ERR_SSL_PROTOCOL_ERROR"**:
- Estás usando HTTPS en lugar de HTTP
- Usa `http://localhost:8098` (no `https://`)

---

## 📚 Documentación Completa

Para más detalles, revisa:
- **`GUIA_COMPLETA_DEBUGGING.md`**: Guía exhaustiva de debugging
- **`CONFIGURAR_VISUAL_STUDIO.md`**: Configuración para Visual Studio

---

## 🎯 Comandos Esenciales

### Detener los Proyectos

En cada terminal, presiona: **Ctrl+C**

### Reiniciar Todo

```powershell
# 1. Detener todos los procesos
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Limpiar y recompilar
cd E:\Proyectos\PoC_Analisis_Desarrollo
dotnet clean
dotnet build

# 3. Volver a iniciar (pasos 3 y 4 arriba)
```

### Ver Logs

Los logs aparecen en las consolas donde ejecutaste `dotnet run`.

### Verificar Puertos

```powershell
# Ver qué procesos usan los puertos
Get-NetTCPConnection -LocalPort 8096 -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 8098 -ErrorAction SilentlyContinue
```

---

## ✅ Checklist de Verificación

Antes de intentar usar el sistema, verifica:

- [ ] Gateway.Api está corriendo (`http://localhost:8096` responde)
- [ ] Gateway.Blazor está corriendo (`http://localhost:8098` responde)
- [ ] No hay errores en las consolas
- [ ] Swagger está accesible (`http://localhost:8096/swagger`)

---

**¿Problemas?** Revisa la sección "🛠️ Si Algo No Funciona" arriba o consulta `GUIA_COMPLETA_DEBUGGING.md`.

