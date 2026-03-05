# ROADMAP – AutoDev MVP

## Resumen ejecutivo

**AutoDev MVP** es una nueva etapa del Sistema Multi-Agente que introduce un **módulo DevFlow**: un flujo de desarrollo guiado por agentes donde una idea de cambio pasa por etapas **UR → PM → PO → DEV**, se persisten los artefactos generados (JSON) y se exige **aprobación humana** entre etapas. Como salida final, se genera un **Branch Plan** (una rama git por tarea).

**Cada tarea se implementa en una rama git independiente.** Este documento es la base de conocimiento para implementar el trabajo.

---

## Alcance / Fuera de alcance (Scope)

### Dentro del alcance (In)
- Modelos de datos: `DevFlowRun`, `DevFlowArtifact`, enums de etapas y estados
- API REST para crear runs, obtener runs, ejecutar etapas y aprobar/rechazar gates
- Pipeline que orquesta agentes UR → PM → PO → DEV en secuencia
- Persistencia de artefactos por etapa en PostgreSQL (JSON)
- Gates de aprobación humana entre etapas
- Modelo Branch Plan y generador de nombres de rama
- Exportación del Branch Plan (JSON/Markdown)
- UI Blazor básica (opcional, última prioridad)

### Fuera del alcance (Out)
- Microservicios o event bus (NATS); queda para etapa futura
- Cambios en la infraestructura de agentes existente (se reutiliza)
- Schemas JSON estrictos por agente (recomendado para siguiente iteración)
- Integración real con Git (creación efectiva de ramas); solo se genera el plan
- Agente UX en este flujo (UR → PM → PO → DEV)

---

## Definiciones

| Término | Definición |
|--------|------------|
| **DevFlowRun** | Una ejecución completa del flujo: idea inicial, estado global, etapas, artefactos. Corresponde a un “proyecto de cambio” o “run”. |
| **DevFlowArtifact** | Output persistido de una etapa (ej. salida del PM, PO, DEV). Contenido en JSON. Vinculado a un `DevFlowRun` y una etapa. |
| **Stage** | Etapa del pipeline: UR, PM, PO, DEV. Cada etapa se ejecuta secuencialmente y produce un artefacto. |
| **Gate** | Punto de control humano entre etapas. Antes de pasar a la siguiente etapa, un usuario debe aprobar. Si rechaza, el run puede pausarse o cancelarse. |
| **Branch Plan** | Plan de ramas git sugerido: una rama por tarea/entregable identificada en el análisis. Se genera al final del flujo o por etapa. Formato: lista de nombres de rama + descripción. |

---

## Hito 5 – AutoDev MVP

### Subtareas internas (orden de implementación)

1. **Persistencia DevFlow** – Modelos y migraciones
2. **API básica** – Crear y consultar runs
3. **Pipeline UR → PM → PO → DEV** – Orquestación e integración de agentes
4. **Ejecución de etapa y persistencia de artefactos**
5. **Aprobaciones humanas (gates)**
6. **Branch Plan** – Modelo, generador y export
7. **UI DevFlow (Blazor)** – Opcional, última prioridad

---

## Historias de usuario

### HU 5.1 – Persistir un DevFlow Run

**Como** usuario del sistema  
**Quiero** crear un DevFlow Run con una idea inicial  
**Para** iniciar un flujo de desarrollo guiado por agentes

**Criterios de aceptación:**
- [ ] AC1: Se almacena en PostgreSQL un `DevFlowRun` con idea inicial, estado, timestamps y userId
- [ ] AC2: El run se asocia al usuario autenticado (Identity)
- [ ] AC3: Estado inicial: `Created` o `Pending`

---

### HU 5.2 – Consultar un DevFlow Run

**Como** usuario del sistema  
**Quiero** obtener los detalles de un DevFlow Run (etapas, artefactos, estado)  
**Para** seguir el progreso y revisar outputs

**Criterios de aceptación:**
- [ ] AC1: Endpoint `GET /api/devflow/runs/{id}` devuelve el run con artefactos
- [ ] AC2: Solo el creador o usuarios con permisos pueden ver el run
- [ ] AC3: Incluye estado de cada etapa y de los gates

---

### HU 5.3 – Ejecutar etapas UR → PM → PO → DEV

**Como** usuario del sistema  
**Quiero** que el pipeline ejecute una etapa (UR, PM, PO o DEV) y guarde el output  
**Para** avanzar el análisis y el diseño técnico de forma automática

**Criterios de aceptación:**
- [ ] AC1: Endpoint `POST /api/devflow/runs/{id}/execute-stage` recibe la etapa a ejecutar
- [ ] AC2: Se invoca al agente correspondiente con el contexto adecuado
- [ ] AC3: El output se persiste como `DevFlowArtifact` en JSON
- [ ] AC4: Las etapas solo pueden ejecutarse en orden: UR → PM → PO → DEV
- [ ] AC5: No se puede ejecutar etapa si el gate previo no está aprobado

---

### HU 5.4 – Aprobar o rechazar entre etapas (Gates)

**Como** usuario responsable  
**Quiero** aprobar o rechazar el paso a la siguiente etapa  
**Para** controlar la calidad y validar antes de continuar

**Criterios de aceptación:**
- [ ] AC1: Endpoint `POST /api/devflow/runs/{id}/gates/{stage}/approve` con body `{ approved: true/false, comment?: string }`
- [ ] AC2: Si se rechaza, el run puede quedar pausado o cancelado
- [ ] AC3: Si se aprueba, se permite ejecutar la siguiente etapa
- [ ] AC4: Solo usuarios autorizados (creador, Admin, SuperUsuario) pueden aprobar

---

### HU 5.5 – Obtener Branch Plan

**Como** desarrollador  
**Quiero** un Branch Plan generado a partir de las salidas de los agentes  
**Para** crear ramas git siguiendo una convención coherente

**Criterios de aceptación:**
- [ ] AC1: El Branch Plan incluye lista de tareas con nombre de rama sugerido y descripción
- [ ] AC2: Nombres de rama siguen convención (ej. `feature/xxx`, `fix/xxx`)
- [ ] AC3: Endpoint `GET /api/devflow/runs/{id}/branch-plan` devuelve el plan en JSON
- [ ] AC4: Opción de exportar en Markdown

---

### HU 5.6 – UI para DevFlow (opcional)

**Como** usuario del sistema  
**Quiero** una interfaz Blazor para crear runs, ver etapas y aprobar  
**Para** usar DevFlow sin depender solo de la API

**Criterios de aceptación:**
- [ ] AC1: Página para listar mis runs
- [ ] AC2: Página para crear un run con idea inicial
- [ ] AC3: Vista de etapa con artefacto y botones Aprobar/Rechazar
- [ ] AC4: Visualización del Branch Plan

---

## Tareas técnicas por historia

### 5.1.1 – Entidad DevFlowRun y enums base

**Objetivo:** Definir el modelo de datos para un run de DevFlow.

**Pasos concretos:**
1. Crear enums `DevFlowStage` (UR, PM, PO, DEV) y `DevFlowRunStatus` (Created, InProgress, Paused, Completed, Cancelled)
2. Crear entidad `DevFlowRun` con: Id, UserId (FK a Identity), Idea, Status, CurrentStage, CreatedAt, UpdatedAt
3. Configurar en `ApplicationDbContext`
4. Crear migración EF Core para PostgreSQL

**Archivos/áreas afectadas:** Backend, DB (Gateway.Api)

**Consideraciones:**
- Usar `ApplicationUser` o `UserId` (int) según Identity existente
- PostgreSQL, no SQLite

**Tests esperados:** Tests unitarios del modelo; migración aplica sin errores.

**Done:** Migración aplicada, entidad mapeada correctamente.

---

### 5.1.2 – Entidad DevFlowArtifact

**Objetivo:** Definir el modelo para artefactos (outputs por etapa).

**Pasos concretos:**
1. Crear entidad `DevFlowArtifact` con: Id, DevFlowRunId (FK), Stage (enum), ContentJson (string/text), CreatedAt
2. Configurar relación 1:N (Run → Artifacts) en DbContext
3. Crear migración EF Core

**Archivos/áreas afectadas:** Backend, DB (Gateway.Api)

**Consideraciones:**
- `ContentJson` como `string` o `JsonDocument` según preferencia; EF Core guarda como text/jsonb
- Posibilidad de índice por (RunId, Stage) para consultas rápidas

**Tests esperados:** Migración correcta; puede insertar y recuperar artefactos.

**Done:** Migración aplicada, artefactos persistibles.

---

### 5.1.3 – Entidad DevFlowGate (aprobaciones)

**Objetivo:** Modelar el estado de aprobación entre etapas.

**Pasos concretos:**
1. Crear entidad `DevFlowGate` con: Id, DevFlowRunId, Stage (etapa tras la cual se requiere aprobación), Status (Pending, Approved, Rejected), ApprovedByUserId, ApprovedAt, Comment
2. Configurar en DbContext y crear migración
3. Un gate por transición (ej. gate tras UR para pasar a PM)

**Archivos/áreas afectadas:** Backend, DB (Gateway.Api)

**Consideraciones:**
- Alternativa: campo en `DevFlowRun` o tabla separada; se opta por tabla para historial

**Tests esperados:** Migración correcta; CRUD de gates.

**Done:** Migración aplicada, gates persistibles.

---

### 5.2.1 – Endpoint POST crear run

**Objetivo:** Crear un DevFlow Run desde la API.

**Pasos concretos:**
1. Crear `POST /api/devflow/runs` con body `{ idea: string }`
2. Crear DTO `CreateDevFlowRunRequest` y `DevFlowRunDto`
3. Servicio `IDevFlowService` con método `CreateRunAsync(userId, idea)`
4. Validar autenticación (Authorize)
5. Retornar run creado con 201 Created

**Archivos/áreas afectadas:** Backend (Gateway.Api)

**Consideraciones:**
- Usar `GetLegacyUserIdAsync` o claims de Identity según implementación actual

**Tests esperados:** Test de integración con WebApplicationFactory; run creado con status correcto.

**Done:** Endpoint funcional, test pasando.

---

### 5.2.2 – Endpoint GET run (por ID)

**Objetivo:** Obtener un run por ID.

**Pasos concretos:**
1. Crear `GET /api/devflow/runs/{id}`
2. Verificar que el usuario puede ver el run (creador o Admin)
3. Incluir artefactos y gates en la respuesta (eager load o DTO con nested data)
4. Retornar 404 si no existe o sin permiso

**Archivos/áreas afectadas:** Backend (Gateway.Api)

**Tests esperados:** Test de integración; 404 para run inexistente; 200 con datos correctos.

**Done:** Endpoint funcional, test pasando.

---

### 5.2.3 – Endpoint GET listar runs

**Objetivo:** Listar los DevFlow Runs del usuario (para UI).

**Pasos concretos:**
1. Crear `GET /api/devflow/runs` con query params opcionales: `status`, `limit`, `offset`
2. Filtrar por UserId del usuario autenticado (o todos si Admin)
3. Retornar lista paginada de runs (sin artefactos completos para evitar payload grande)
4. Ordenar por CreatedAt descendente

**Archivos/áreas afectadas:** Backend (Gateway.Api)

**Tests esperados:** Test de integración; filtros y paginación.

**Done:** Endpoint funcional, test pasando.

**Pasos concretos:**
1. Crear `GET /api/devflow/runs/{id}`
2. Verificar que el usuario puede ver el run (creador o Admin)
3. Incluir artefactos y gates en la respuesta (eager load o DTO con nested data)
4. Retornar 404 si no existe o sin permiso

**Archivos/áreas afectadas:** Backend (Gateway.Api)

**Tests esperados:** Test de integración; 404 para run inexistente; 200 con datos correctos.

**Done:** Endpoint funcional, test pasando.

---

### 5.3.1 – DevFlowPipeline (orquestador de etapas)

**Objetivo:** Componente que conoce el orden UR → PM → PO → DEV y coordina la ejecución.

**Pasos concretos:**
1. Crear clase `DevFlowPipeline` en proyecto Gateway.Api o nuevo `DevFlow.Core`
2. Método `GetNextStage(currentStage)` que devuelve la siguiente etapa
3. Método `GetStages()` que devuelve el orden completo
4. Inyectar en DI como singleton o scoped

**Archivos/áreas afectadas:** Backend

**Consideraciones:**
- Sin ejecución real aún; solo lógica de orden

**Tests esperados:** Tests unitarios del orden de etapas.

**Done:** Pipeline definido, tests pasando.

---

### 5.3.2 – Integración con agentes existentes

**Objetivo:** Conectar el pipeline con los agentes UR, PM, PO, DEV.

**Pasos concretos:**
1. Resolver `IAgent` por `AgentRole` en el servicio DevFlow
2. Construir `AgentTurn` con el contexto acumulado (mensaje usuario + salidas previas)
3. Invocar `agent.HandleAsync(turn)` y capturar `ChatMessage`
4. Mapear salida a estructura para artefacto (por ahora JSON genérico)

**Archivos/áreas afectadas:** Backend, agentes (uso existente)

**Consideraciones:**
- Reutilizar `IBehaviorProvider` para prompts; sin cambios en behaviors
- No crear microservicios; todo en-process

**Tests esperados:** Test que simule agente y verifique que se invoca con contexto correcto.

**Done:** Integración funcional; ejecución manual verificada.

---

### 5.4.1 – Endpoint ejecutar etapa

**Objetivo:** Endpoint que ejecuta una etapa y persiste el artefacto.

**Pasos concretos:**
1. Crear `POST /api/devflow/runs/{id}/execute-stage` con body `{ stage: "PM" }` (ejemplo)
2. Validar que el run existe, está en estado ejecutable y la etapa es la siguiente válida
3. Validar que el gate previo (si aplica) está aprobado
4. Invocar pipeline + agente, obtener salida
5. Crear `DevFlowArtifact` con la salida en JSON y guardar
6. Actualizar `DevFlowRun.CurrentStage` y `Status`
7. Crear gate para la siguiente etapa (si aplica)
8. Retornar run actualizado

**Archivos/áreas afectadas:** Backend (Gateway.Api)

**Consideraciones:**
- Serializar `ChatMessage.Text` u objeto estructurado a JSON
- Schemas JSON por agente: pendiente para siguiente iteración

**Tests esperados:** Test de integración; artefacto persistido; run actualizado.

**Done:** Endpoint funcional, artefactos persistidos correctamente.

---

### 5.5.1 – Endpoint aprobar/rechazar gate

**Objetivo:** Endpoint para aprobación humana entre etapas.

**Pasos concretos:**
1. Crear `POST /api/devflow/runs/{id}/gates/{stage}/approve` con body `{ approved: bool, comment?: string }`
2. Buscar gate por run y etapa; validar que está Pending
3. Validar que el usuario tiene permiso (creador, Admin, SuperUsuario)
4. Actualizar gate con Status, ApprovedByUserId, ApprovedAt, Comment
5. Si aprobado: marcar run listo para siguiente etapa
6. Si rechazado: opcionalmente pausar o cancelar run
7. Retornar run actualizado

**Archivos/áreas afectadas:** Backend (Gateway.Api)

**Tests esperados:** Test de integración; gate aprobado/rechazado; run actualizado.

**Done:** Endpoint funcional, tests pasando.

---

### 5.6.1 – Modelo Branch Plan

**Objetivo:** Definir el modelo de datos para el Branch Plan.

**Pasos concretos:**
1. Crear DTO/clases `BranchPlan`, `BranchPlanItem` con: BranchName, Description, (opcional) Order
2. Sin persistencia aún; solo estructura en memoria
3. Documentar convención de nombres (ej. `feature/descripcion-corta`, `fix/issue-123`)

**Archivos/áreas afectadas:** Backend (Gateway.Api, DTOs)

**Tests esperados:** Test unitario de construcción del modelo.

**Done:** Modelo definido y serializable a JSON.

---

### 5.6.2 – Generador de nombres de rama

**Objetivo:** Generar nombres de rama a partir del contenido de artefactos.

**Pasos concretos:**
1. Crear `IBranchNameGenerator` con método `GenerateBranchName(description, index?)` 
2. Implementación que sanitiza y acorta texto (slug): minúsculas, sin espacios, max N caracteres
3. Prefijo configurable: `feature/`, `fix/`, etc.
4. Evitar colisiones: añadir sufijo numérico si es necesario

**Archivos/áreas afectadas:** Backend

**Consideraciones:**
- Extraer "tareas" de artefactos: por ahora, usar el texto del DEV o un prompt adicional; simplificar a líneas/ítems si el output está estructurado

**Tests esperados:** Tests unitarios con distintos inputs; nombres válidos para git.

**Done:** Generador funcional, tests pasando.

---

### 5.6.3 – Servicio Branch Plan y export

**Objetivo:** Generar Branch Plan a partir de los artefactos del run y exportarlo.

**Pasos concretos:**
1. Crear método `IDevFlowService.GetBranchPlanAsync(runId)` 
2. Leer artefactos del run; parsear o extraer tareas (heurística o prompt futuro)
3. Usar `IBranchNameGenerator` para cada tarea
4. Construir `BranchPlan` y retornar
5. Crear `GET /api/devflow/runs/{id}/branch-plan` que devuelve JSON
6. Crear `GET /api/devflow/runs/{id}/branch-plan?format=md` que devuelve Markdown

**Archivos/áreas afectadas:** Backend (Gateway.Api)

**Consideraciones:**
- Extracción de tareas: primera versión puede ser líneas del artefacto DEV o ítems numerados; en el futuro, schema JSON o LLM

**Tests esperados:** Test que verifique estructura del plan; export Markdown correcto.

**Done:** Endpoints funcionales, plan generado correctamente.

---

### 5.7.1 – UI página DevFlow (opcional)

**Objetivo:** Página Blazor para listar runs.

**Pasos concretos:**
1. Crear página `DevFlow.razor` en `/devflow` o `/admin/devflow`
2. Llamar a `GET /api/devflow/runs` (crear endpoint list si no existe)
3. Mostrar tabla con: Id, Idea (truncado), Status, CreatedAt
4. Enlace desde NavMenu (Admin o todos los autenticados según política)

**Archivos/áreas afectadas:** Frontend (Gateway.Blazor)

**Tests esperados:** Manual o E2E opcional.

**Done:** Página carga y muestra runs.

---

### 5.7.2 – UI crear run (opcional)

**Objetivo:** Formulario para crear un run.

**Pasos concretos:**
1. Componente o sección en DevFlow para "Nuevo run"
2. Campo texto para Idea
3. POST al crear y redirigir a detalle del run

**Archivos/áreas afectadas:** Frontend (Gateway.Blazor)

**Done:** Creación funcional desde UI.

---

### 5.7.3 – UI vista etapa y aprobación (opcional)

**Objetivo:** Ver artefacto de una etapa y aprobar/rechazar.

**Pasos concretos:**
1. Página o sección de detalle de run con etapas
2. Por cada etapa: mostrar artefacto (JSON formateado o Markdown si aplica)
3. Si hay gate pendiente: botones Aprobar / Rechazar
4. Llamar a endpoint de aprobación; actualizar vista

**Archivos/áreas afectadas:** Frontend (Gateway.Blazor)

**Done:** Aprobación funcional desde UI.

---

### 5.7.4 – UI visualización Branch Plan (opcional)

**Objetivo:** Mostrar el Branch Plan en la interfaz.

**Pasos concretos:**
1. Sección en detalle de run para Branch Plan
2. Llamar a `GET /api/devflow/runs/{id}/branch-plan`
3. Mostrar lista de ramas con descripción
4. Botón "Copiar" o "Exportar Markdown"

**Archivos/áreas afectadas:** Frontend (Gateway.Blazor)

**Done:** Branch Plan visible y exportable desde UI.

---

## Ramas por tarea (resumen)

| Rama | Tarea asociada |
|------|-----------------|
| `feature/autodev/devflow-enums` | 5.1.1 – Enums DevFlowStage, DevFlowRunStatus |
| `feature/autodev/devflow-run-entity` | 5.1.1 – Entidad DevFlowRun + migración |
| `feature/autodev/devflow-artifact-entity` | 5.1.2 – Entidad DevFlowArtifact + migración |
| `feature/autodev/devflow-gate-entity` | 5.1.3 – Entidad DevFlowGate + migración |
| `feature/autodev/devflow-create-run-endpoint` | 5.2.1 – POST crear run |
| `feature/autodev/devflow-get-run` | 5.2.2 – GET run por ID |
| `feature/autodev/devflow-list-runs` | 5.2.3 – GET listar runs |
| `feature/autodev/devflow-pipeline` | 5.3.1 – DevFlowPipeline |
| `feature/autodev/devflow-agent-integration` | 5.3.2 – Integración agentes |
| `feature/autodev/devflow-execute-stage` | 5.4.1 – POST ejecutar etapa |
| `feature/autodev/devflow-approval-endpoint` | 5.5.1 – POST aprobar gate |
| `feature/autodev/branch-plan-model` | 5.6.1 – Modelo BranchPlan |
| `feature/autodev/branch-name-generator` | 5.6.2 – Generador nombres |
| `feature/autodev/branch-plan-export` | 5.6.3 – Servicio y endpoints export |
| `feature/autodev/devflow-ui-page` | 5.7.1 – Página listar runs |
| `feature/autodev/devflow-ui-create-run` | 5.7.2 – UI crear run |
| `feature/autodev/devflow-ui-stage-view` | 5.7.3 – UI etapa y aprobación |
| `feature/autodev/devflow-ui-approval` | 5.7.3 – (mismo alcance que stage-view) |
| `feature/autodev/devflow-ui-branch-plan` | 5.7.4 – UI Branch Plan |

---

## Dependencias entre tareas

```
5.1.1 (enums + DevFlowRun) ──┬──► 5.2.1 (POST create)
                             └──► 5.2.2 (GET run)

5.1.2 (DevFlowArtifact) ──────► 5.4.1 (execute stage)

5.1.3 (DevFlowGate) ───────────► 5.5.1 (approval endpoint)

5.2.1, 5.2.2 ─────────────────► 5.4.1 (execute stage usa run)

5.3.1 (Pipeline) ─────────────► 5.3.2 (agent integration)
       │                              │
       └──────────────────────────────┴──► 5.4.1 (execute stage)

5.4.1 (execute stage) ────────► 5.6.1, 5.6.2, 5.6.3 (Branch Plan)

5.5.1 (approval) ─────────────► 5.4.1 (validación de gate antes de ejecutar)

5.6.1 ──► 5.6.2 ──► 5.6.3 (Branch Plan en secuencia)

5.2.1, 5.2.2, 5.4.1, 5.5.1 ──► 5.7.x (UI consume endpoints)
```

**Diagrama ASCII simplificado:**

```
[5.1.1 Run+Enums] ──► [5.1.2 Artifact] ──► [5.4.1 Execute]
        │                     │
        ▼                     │
[5.2.1 Create] ──────────────┤
[5.2.2 Get] ─────────────────┤
        │                     │
        ▼                     │
[5.3.1 Pipeline] ──► [5.3.2 Agents] ────► [5.4.1]
        │
[5.1.3 Gate] ──► [5.5.1 Approval] ─────► [5.4.1]
                          │
                          ▼
[5.6.1 Model] ──► [5.6.2 Generator] ──► [5.6.3 Export]
                          │
                          ▼
                  [5.7.x UI opcional]
```

---

## Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| Artefactos muy grandes en JSON | Lentitud, límites DB | Limitar tamaño; comprimir o truncar si excede N caracteres |
| Prompts de agentes poco adaptados a DevFlow | Salidas inconsistentes | Ajustar prompts en Behaviors; añadir instrucción "salida estructurada para DevFlow" |
| Extracción de tareas para Branch Plan imprecisa | Plan de baja calidad | Primera versión con heurística simple; iterar con schema JSON o prompt dedicado |
| Gates bloqueados sin aprobador | Run bloqueado | Definir tiempo máximo; notificaciones (futuro); Admin puede desbloquear |
| Migraciones en Docker con volumen existente | Conflictos pgdata | Documentar recreación de volumen si se cambia schema de forma incompatible |

---

## Definition of Done (checklist para PR)

Para cada PR de una tarea de este roadmap:

- [ ] Código compila sin errores ni warnings relevantes
- [ ] Tests asociados a la tarea pasan (unit/integración)
- [ ] Migraciones EF aplican correctamente en PostgreSQL (si aplica)
- [ ] Endpoints documentados en Swagger (si aplica)
- [ ] Sin cambios en configuraciones sensibles (secrets, conexiones) sin documentar
- [ ] Revisión de código completada
- [ ] Documentación actualizada si la tarea añade conceptos nuevos
- [ ] Rama mergeada a `main`/`develop` según flujo del equipo

---

*Documento de planificación AutoDev MVP. Base de conocimiento para implementación. No incluye código.*
