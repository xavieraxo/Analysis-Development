# Posibles Causas de Lentitud al Iniciar

## 🔴 Causa Principal: Recreación de Base de Datos

**Ubicación:** `src/Gateway.Api/Program.cs`

```csharp
db.Database.Migrate(); // Aplica migraciones pendientes
```

### Problema:
- **Recrear esquema** en cada inicio era costoso y bloqueaba el arranque
- Esto puede tomar varios segundos dependiendo de:
  - Tamaño del esquema (tablas, índices, foreign keys)
  - Complejidad del modelo de datos
  - Operaciones de I/O del sistema de archivos

### Solución Recomendada:
Usar migraciones de EF Core en lugar de recrear la base de datos:

```csharp
// Usar migraciones (más eficiente)
db.Database.Migrate(); // Aplica solo cambios necesarios
```

## ⚠️ Otras Posibles Causas

### 1. Configuración de HttpClient con Timeout Alto
**Ubicación:** `src/Gateway.Api/Program.cs` (línea 60)

```csharp
var timeoutSeconds = builder.Configuration.GetValue<int>("OpenAI:TimeoutSeconds", 600);
```

- Timeout de 600 segundos (10 minutos) no debería afectar el inicio
- Pero si intenta validar la conexión a Ollama al iniciar, podría causar retraso

**Solución:** Verificar si hay validación de conectividad al iniciar y deshabilitarla.

### 2. Inicialización de Servicios Pesados
**Ubicación:** `src/Gateway.Api/Program.cs` (líneas 86-98)

- Múltiples agentes registrados como Singletons
- Orchestrator con dependencias
- Estos se inicializan al arrancar la aplicación

**Impacto:** Normalmente mínimo, pero podría contribuir a la lentitud.

### 3. Configuración de Blazor Server
**Ubicación:** `src/Gateway.Blazor/Program.cs` (líneas 12-18)

```csharp
options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
```

- Timeouts altos no deberían afectar el inicio directamente
- Podría indicar configuración demasiado conservadora

### 4. JWT Token Validation
**Ubicación:** `src/Gateway.Api/Program.cs` (líneas 36-49)

- Validación de JWT configurada, pero no debería ejecutarse al iniciar
- Solo se ejecuta cuando hay una petición con token

### 5. Conexión a Base de Datos PostgreSQL
- Postgres es estable, pero la conexión puede demorar si el contenedor aún no está listo

## 📊 Prioridades para Optimizar

### Prioridad ALTA (Impacto inmediato):
1. **Comentar o cambiar `EnsureDeleted()`** - Esta es la causa más probable
2. **Usar migraciones de EF Core** en lugar de recrear la BD

### Prioridad MEDIA:
3. **Optimizar el esquema de base de datos** si es muy complejo
4. **Verificar si hay validaciones al iniciar** que intenten conectarse a servicios externos

### Prioridad BAJA:
5. Revisar configuración de servicios singleton
6. Ajustar timeouts si es necesario

## 🔧 Solución Rápida

Para probar rápidamente si el problema es `EnsureDeleted()`, puedes comentar esa línea temporalmente:

```csharp
// Aplicar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // En desarrollo, eliminar y recrear la base de datos si el esquema cambió
    // En producción, usar migraciones de EF Core
    if (app.Environment.IsDevelopment())
    {
        // COMENTAR ESTA LÍNEA PARA PROBAR:
        // db.Database.EnsureDeleted(); // Eliminar base de datos existente
    }
    db.Database.EnsureCreated(); // Crear base de datos con el esquema actualizado
}
```

Si al comentar `EnsureDeleted()` el inicio se vuelve más rápido, esa es la causa principal.

