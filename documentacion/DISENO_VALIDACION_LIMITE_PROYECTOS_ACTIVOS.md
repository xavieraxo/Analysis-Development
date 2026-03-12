# Diseño técnico: Validación del límite de proyectos activos por plan

Definición precisa de **dónde** y **cómo** se valida el límite de proyectos activos según la suscripción, integrado a la arquitectura actual. Sin implementación; solo diseño.

---

## Regla cerrada (acordada)

- Los límites aplican **solo a proyectos activos**.
- **Activos:** `ProjectStatus.InProgress`, `ProjectStatus.OnHold`.
- **No contables (cerrados):** `ProjectStatus.Completed`, `ProjectStatus.Cancelled`.
- **Límites por plan:**
  - **Free:** 1 activo.
  - **Plan 1:** 1 activo.
  - **Plan 2:** 3 activos.
  - **Planes empresa:** según `SubscriptionPlan.MaxActiveProjects` (Plan 3 = 5, Plan 4 = 10, Plan 5 = ilimitado/null).
- Los proyectos cerrados (Completed, Cancelled) **no cuentan** y pueden ser ilimitados en cantidad.

---

# 1. En qué servicio debe validarse

**Servicio:** **IPlanLimitService** (nuevo), implementado por **PlanLimitService**.

- **Responsabilidad única para esta regla:** decidir si un usuario o una empresa puede **tener un proyecto más en estado activo** (o reabrir uno a activo), según el plan efectivo y el conteo actual de proyectos activos.
- La validación **no** va en `ProjectService`: `ProjectService` orquesta creación y actualización de proyecto/run; **invoca** a `IPlanLimitService` antes de ejecutar la acción que aumentaría el número de proyectos activos. Así la lógica de “límite por plan” queda en un solo lugar y se reutiliza desde creación, reapertura y (si aplica) cambio de plan.

**Resumen:** validación en **IPlanLimitService**; **ProjectService** y el endpoint que cambia estado **llaman** a ese servicio y reaccionan al resultado (éxito o error de negocio).

---

# 2. Interfaz nueva: IPlanLimitService

**Interfaz recomendada:**

- **Nombre:** `IPlanLimitService`.
- **Ensamblado / capa:** mismo que los demás servicios de dominio de la API (ej. `Gateway.Api.Services`).

**Métodos que debe exponer para proyectos activos:**

| Método | Propósito |
|--------|-----------|
| `Task<ActiveProjectLimitResult> CheckActiveProjectLimitAsync(ActiveProjectLimitRequest request, CancellationToken cancellationToken = default)` | Consulta: dado un usuario y un contexto (crear proyecto individual vs crear proyecto de empresa), devuelve si puede crear/activar un proyecto más y datos para UI (actual, máximo, plan). No lanza excepción; devuelve un resultado. |
| `Task EnsureCanAddActiveProjectAsync(ActiveProjectLimitRequest request, CancellationToken cancellationToken = default)` | Validación “todo o nada”: si **no** puede agregar un proyecto activo, lanza la excepción de negocio acordada (véase punto 6). Si puede, no hace nada. Es el método que debe llamar `ProjectService` antes de crear o antes de reabrir. |

**Parámetros (request):**

- **ActiveProjectLimitRequest** (DTO o tipo análogo):
  - **CurrentUserId** (int): usuario que ejecuta la acción (para resolver plan efectivo y ownership).
  - **OwnerType** (Individual | Empresa): si el proyecto a crear/reabrir es individual o de empresa.
  - **OwnerUserId** (int?, solo si OwnerType = Individual): dueño del proyecto individual (normalmente = CurrentUserId).
  - **EmpresaId** (int?, solo si OwnerType = Empresa): empresa dueña del proyecto.

**Conteo de “activos”:**

- El servicio cuenta proyectos donde:
  - **Individual:** `Project.OwnerType == Individual && Project.OwnerUserId == request.OwnerUserId` (o `UserId` en modelo legacy) y `Status` ∈ { InProgress, OnHold }.
  - **Empresa:** `Project.OwnerType == Empresa && Project.EmpresaId == request.EmpresaId` y `Status` ∈ { InProgress, OnHold }.
- Obtiene el plan efectivo vía **ISubscriptionService** (GetEffectivePlanForUserAsync / plan por empresa) y el **MaxActiveProjects** del plan (Free=1, Plan1=1, Plan2=3, empresa según catálogo). Si el plan es null o no tiene suscripción, se trata como **Free** (1 activo para individual); empresa sin suscripción según regla de negocio (bloquear o plan mínimo).

**Resultado de consulta (ActiveProjectLimitResult):**

- **CanAdd:** bool.
- **CurrentActiveCount:** int.
- **MaxAllowed:** int? (null = ilimitado).
- **PlanCode:** string (para mensajes y UI).
- **LimitExceededReason:** string? (opcional, mensaje corto cuando CanAdd = false).

---

# 3. Qué método debe llamar ProjectService al crear Project + DevFlowRun

**Flujo actual (referencia):**  
`CreateProjectWithInitialDevFlowRunAsync(CreateProjectRequest request, int currentUserId)` hoy comprueba “un solo proyecto activo” para el usuario con lógica propia. Esa comprobación debe **sustituirse** por la llamada al nuevo servicio de límites.

**Método a llamar:**  
`EnsureCanAddActiveProjectAsync(ActiveProjectLimitRequest request, CancellationToken cancellationToken)`.

**Momento:**  
**Antes** de abrir transacción (o como primera operación dentro de la misma): después de validar que el request es válido (nombre, etc.), y **antes** de crear la entidad `Project` y el `DevFlowRun`.

**Request a armar:**

- Si el flujo es “crear proyecto **individual**” (caso actual del endpoint que usa `CreateProjectWithInitialDevFlowRunAsync`):
  - **CurrentUserId** = currentUserId.
  - **OwnerType** = Individual.
  - **OwnerUserId** = currentUserId.
  - **EmpresaId** = null.
- Si en el futuro el mismo método (u otro) permite “crear proyecto de **empresa**”:
  - **CurrentUserId** = currentUserId (quien ejecuta).
  - **OwnerType** = Empresa.
  - **OwnerUserId** = null.
  - **EmpresaId** = id de la empresa que será dueña.

**Comportamiento:**

- Si `EnsureCanAddActiveProjectAsync` **no** lanza → `ProjectService` sigue con la transacción (crear Project en InProgress, crear DevFlowRun, commit).
- Si **lanza** → no se crea nada; la excepción se propaga al endpoint (véase punto 4).

**Resumen:**  
`ProjectService.CreateProjectWithInitialDevFlowRunAsync` debe llamar **una sola vez**, al inicio, a `IPlanLimitService.EnsureCanAddActiveProjectAsync` con el request adecuado según ownership del proyecto a crear. No debe duplicar la lógica de “cuántos activos” ni “cuál es el máximo”; eso es responsabilidad de `IPlanLimitService`.

---

# 4. Qué debe hacer el endpoint

**Endpoint actual (referencia):**  
`POST /api/projects` (o el que invoca `CreateProjectWithInitialDevFlowRunAsync`) recibe el request, obtiene el userId del token, llama a `ProjectService.CreateProjectWithInitialDevFlowRunAsync` y devuelve el DTO del proyecto + run.

**Qué debe hacer:**

1. **Autorización:** sin cambio; seguir exigiendo usuario autenticado (y si aplica, política de acceso).
2. **Llamada al servicio:** invocar `CreateProjectWithInitialDevFlowRunAsync` (que internamente llama a `EnsureCanAddActiveProjectAsync`).
3. **Manejo de la excepción de límite:** cuando el servicio lance la excepción de negocio definida para “límite de proyectos activos excedido” (véase punto 6), el endpoint debe:
   - **Capturarla** de forma explícita (por tipo de excepción o por código de error).
   - **Responder con HTTP 403 Forbidden** (o 409 Conflict si se prefiere “conflicto con estado actual”).
   - **Cuerpo de la respuesta:** mensaje claro para la UI, por ejemplo:  
     `{ "code": "ActiveProjectLimitExceeded", "message": "Has alcanzado el límite de proyectos activos (Plan Free). Cierra o cancela un proyecto para crear otro.", "currentCount": 1, "maxAllowed": 1, "planCode": "Free" }`  
     (usando los datos que la excepción puede llevar).
4. **Éxito:** sin cambio; 200/201 con el DTO del proyecto y run.

**Resumen:** el endpoint no valida el límite por sí mismo; delega en el servicio. Su única responsabilidad extra es **mapear** la excepción de límite a una respuesta HTTP y cuerpo estándar para que la UI pueda mostrar el mensaje y deshabilitar/indicar “upgrade” o “cerrar un proyecto”.

---

# 5. Qué debe hacer la UI solo como apoyo visual

La UI **no** es fuente de verdad; solo refleja y guía.

- **Antes de crear proyecto:**
  - Si se dispone de un endpoint de “estado de límites” (ej. `GET /api/subscription/limits` o incluido en perfil), mostrar: “Proyectos activos: X / Y” (o “X / ilimitados”). Si X >= Y y Y no es ilimitado, **deshabilitar** el botón “Crear proyecto” y mostrar el mensaje: “Has alcanzado el límite de proyectos activos (Plan Z). Cierra o cancela un proyecto para crear otro.”
  - Si no se tiene ese endpoint, el botón puede estar habilitado; al enviar el POST, si el backend responde 403/409 con `ActiveProjectLimitExceeded`, la UI muestra el mensaje devuelto y no crea el proyecto.
- **Después de intentar crear:**
  - Si la respuesta es 403/409 con el código acordado, mostrar el `message` (y opcionalmente currentCount, maxAllowed, planCode) en un snackbar/alert.
- **Reabrir proyecto:**
  - Si la UI permite “Reabrir” (pasar de Completed/Cancelled a InProgress/OnHold), puede deshabilitar esa acción cuando ya está al límite (si tiene datos de límites); en cualquier caso, si el usuario intenta reabrir y el backend rechaza por límite, mostrar el mismo tipo de mensaje.

La UI **no** debe decidir por sí sola “este usuario no puede crear”; solo debe **ocultar/deshabilitar** o **mostrar error** en función de datos del backend o de la respuesta del endpoint.

---

# 6. Cómo modelar el error de negocio cuando el límite se excede

**Recomendación:** una excepción de negocio dedicada que lleve los datos necesarios para el endpoint y la UI.

**Nombre sugerido:** `ActiveProjectLimitExceededException` (o nombre genérico `PlanLimitExceededException` con un `LimitType = ActiveProjects` si se quieren reutilizar más límites).

**Contenido recomendado (propiedades):**

- **LimitType:** constante o enum (ej. "ActiveProjects").
- **CurrentCount:** int (cuántos proyectos activos tiene ahora).
- **MaxAllowed:** int? (null si ilimitado; en este caso no se lanzaría).
- **PlanCode:** string (plan que tiene el usuario/empresa).
- **Message:** string (mensaje legible, ej. "Has alcanzado el límite de proyectos activos (Plan Free). Cierra o cancela un proyecto para crear otro.").

**Dónde se lanza:**  
En **PlanLimitService.EnsureCanAddActiveProjectAsync**: después de calcular el conteo actual y el máximo del plan, si `CurrentActiveCount >= MaxAllowed` (y MaxAllowed no es null), se lanza esta excepción con los datos anteriores. El endpoint la captura y la convierte en 403/409 + cuerpo JSON.

**Alternativa sin excepción:**  
`EnsureCanAddActiveProjectAsync` podría devolver un resultado (Success / LimitExceeded con datos) y `ProjectService` lanzar la excepción o devolver un resultado fallido. La excepción dedicada suele simplificar el flujo en capas: el servicio de límites “dice no” lanzando y el orquestador no tiene que inspeccionar resultados.

---

# 7. Qué otros casos deben validar el límite además de crear proyecto

Además de **crear proyecto (Project + DevFlowRun)**, deben validar el mismo límite:

1. **Reabrir proyecto (cambio de estado a activo)**  
   - **Dónde:** en **ProjectService.UpdateProjectStatusAsync** (o el método que actualice `Project.Status`).  
   - **Cuándo:** cuando el **nuevo** estado es `InProgress` u `OnHold` y el **estado anterior** era `Completed` o `Cancelled`. En ese caso, al “reabrir” se está sumando un proyecto activo.  
   - **Qué hacer:** antes de aplicar el cambio de estado, llamar a `EnsureCanAddActiveProjectAsync` con el mismo criterio de ownership (si el proyecto es individual, OwnerUserId = dueño; si es empresa, EmpresaId = empresa). Si lanza, no cambiar el estado y propagar la excepción; el endpoint devuelve 403/409 con el mismo esquema.  
   - **Excepción:** si el estado **actual** ya es InProgress/OnHold y se “actualiza” al mismo, no hay que revalidar (no se está agregando un activo).

2. **Cambio de plan (downgrade)**  
   - **Dónde:** en el servicio o endpoint que asigna una nueva suscripción a un usuario o empresa (ej. “cambiar a Plan 1”).  
   - **Qué validar:** si el **nuevo** plan tiene `MaxActiveProjects` **menor** que la cantidad actual de proyectos activos del usuario/empresa, no se puede completar el downgrade sin que se exceda el límite.  
   - **Opciones de diseño:**  
     - **A)** Rechazar el cambio de plan hasta que el usuario/empresa cierre o cancele proyectos hasta quedar por debajo del nuevo máximo (recomendado para MVP).  
     - **B)** Permitir el cambio pero “advertir” y dejar que en el siguiente “crear” o “reabrir” falle hasta que bajen los activos.  
   - **Recomendación:** validar en el momento del cambio de plan: obtener `CurrentActiveCount` y `MaxAllowed` del **nuevo** plan; si `CurrentActiveCount > MaxAllowed`, rechazar la operación con un error claro (“Baja proyectos activos a X o menos antes de cambiar a Plan Y”) y no cambiar la suscripción.

**Resumen de puntos de validación:**

| Acción | Dónde se valida | Método a llamar |
|--------|------------------|------------------|
| Crear proyecto (individual o empresa) | ProjectService.CreateProjectWithInitialDevFlowRunAsync (y cualquier otro método que cree proyecto en InProgress) | EnsureCanAddActiveProjectAsync **antes** de crear |
| Reabrir proyecto (Completed/Cancelled → InProgress/OnHold) | ProjectService.UpdateProjectStatusAsync (o equivalente) | EnsureCanAddActiveProjectAsync **antes** de actualizar el estado |
| Cambio de plan (downgrade) | Servicio/endpoint de asignación de suscripción | Lógica equivalente: comparar CurrentActiveCount con MaxAllowed del **nuevo** plan; si excede, rechazar |

No se valida el límite al **cerrar** un proyecto (Completed/Cancelled): eso siempre está permitido y reduce el conteo de activos.

---

## Resumen de integración

- **Servicio que valida:** `IPlanLimitService` / `PlanLimitService`.
- **Método que orquesta creación/reapertura:** `EnsureCanAddActiveProjectAsync(request)`.
- **ProjectService:** llama a `EnsureCanAddActiveProjectAsync` al inicio de crear proyecto y antes de cambiar estado a InProgress/OnHold cuando viene de cerrado.
- **Endpoint:** captura la excepción de límite y responde 403 (o 409) con cuerpo `ActiveProjectLimitExceeded`.
- **UI:** muestra límites si tiene datos; deshabilita “Crear”/“Reabrir” cuando está al límite; muestra el mensaje del backend en caso de error.
- **Error de negocio:** `ActiveProjectLimitExceededException` con CurrentCount, MaxAllowed, PlanCode, Message.
- **Otros casos:** reabrir proyecto y downgrade de plan validan el mismo límite en los puntos indicados.

Este diseño queda alineado con la regla cerrada (activos = InProgress + OnHold; Free=1, Plan1=1, Plan2=3; planes empresa según catálogo) y con el diseño del módulo de suscripciones ya documentado.
