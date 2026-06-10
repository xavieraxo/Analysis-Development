# PLAN DE EJECUCIÓN – Discovery completo (Etapa 1 del producto)

**Fecha:** Junio 2026
**Origen:** Alineación de la arquitectura actual con la visión definitiva del producto
**Objetivo:** Implementar el Discovery completo para el usuario final **evolucionando** el motor DevFlow existente, sin rediseñar el producto, sin segundo motor de workflow, sin segundo sistema de agentes ni de artefactos, y **preservando AutoDev, DEV y BranchPlan** para la Etapa 2.

---

## Visión del producto (referencia obligatoria)

```
Idea → Discovery → Aprobación → Development → AutoDev → Entregable
```

| Fase de la visión | Componente existente que la cubre | Etapa |
|---|---|---|
| Idea | `Project` + `CreateProjectWithInitialDevFlowRunAsync` (ya existe, falta exponer) | 1 |
| Discovery | `DevFlowRun` `FlowType=Discovery` (UR→PM→PO→UX→PLAN) + `DevFlowArtifacts` | 1 |
| Aprobación | Gates + `PendingApproval` + `ApproveGateAsync` | 1 |
| Development | `FlowType=Development` (UR→PM→PO→DEV) | 2 (preservado) |
| AutoDev | `FlowType=AutoDev` + `DevAgent` | 2 (preservado) |
| Entregable | `BranchPlan` + export JSON/MD (se ampliará: zip/git) | 2 (preservado) |

**Principio rector:** todo lo que ya existe se reutiliza. Cada tarea de este plan extiende o expone componentes actuales; ninguna los reemplaza.

---

## Reglas de trabajo

| Regla | Detalle |
|-------|---------|
| Una tarea = una rama | No mezclar alcances de dos tareas en la misma rama |
| Git | Según [`documentacion/GitRules.md`](../GitRules.md): la IA crea rama y desarrolla; **no ejecuta commit ni push**; sugiere mensaje de commit y espera revisión humana |
| Merge manual | El integrador mergea cada rama a `main` cuando esté lista |
| Orden | Respetar dependencias del diagrama al final |
| Fuera de alcance Etapa 1 | Generación de código DEV, descarga ZIP/git del entregable, módulo de suscripciones completo, RAG |
| Preservar intacto | Pipeline AutoDev (`UR→PM→PO→DEV`), etapa DEV, `DevAgent`, `BranchPlan` y su export, `/api/chat/run` y Orchestrator, mapping `PLAN→PM` |

**Convención de ramas**

```
feature/discovery-mvp/<slug-corto>
```

**Artefacto machine-readable:** [`PLAN_DISCOVERY_MVP.branch-plan.json`](./PLAN_DISCOVERY_MVP.branch-plan.json)

---

## Decisiones de diseño que protegen la Etapa 2

1. **Un solo motor:** toda la Etapa 1 corre sobre `DevFlowRun` / `DevFlowService` / `DevFlowPipeline` / `DevFlowAgentDispatcher` existentes.
2. **Conversación por etapa, no por flujo:** la entidad de mensajes (`DevFlowStageMessage`) se asocia a `(RunId, Stage)`. AutoDev y Development heredan la conversación UR sin código adicional.
3. **Mismo artefacto:** la salida final de UR conversacional es el mismo `DevFlowArtifact` que hoy; PM/PO/UX/PLAN no cambian su contrato.
4. **Mismo gate:** la aprobación final del Discovery (`PendingApproval` → approve) es el mismo punto que en Etapa 2 disparará la creación del run Development sobre el mismo proyecto.
5. **Runs encadenados por proyecto:** Development/AutoDev serán **nuevos runs** sobre el mismo `Project`, con los artefactos del Discovery aprobado como contexto. Nada en Etapa 1 debe impedirlo.
6. **Gate por plan como punto de extensión:** `IPlanLimitService` se introduce con implementación mínima (configurable), alineado al diseño cerrado en `DISENO_VALIDACION_LIMITE_PROYECTOS_ACTIVOS.md`. La Etapa 2 solo sustituye la implementación.

---

## Hito 10 – Idea → Discovery automático y acceso del dueño

**Objetivo del hito:** Al crear un proyecto, el usuario obtiene su run Discovery en etapa UR, y puede consultar su propio run (hoy todo DevFlow es SuperUsuario-only).

**Resultado esperado del hito:** Usuario crea proyecto → existe run Discovery vinculado → el dueño puede verlo vía API. Admin conserva acceso total.

---

### HU 10.1 – Crear proyecto inicia run Discovery

**Como** usuario final
**Quiero** que al crear mi proyecto con su descripción se inicie automáticamente el Discovery
**Para** comenzar el relevamiento sin pasos administrativos

#### Criterios de aceptación

- [ ] AC1: `POST /api/projects` (o endpoint dedicado) crea `Project` + `DevFlowRun` con `FlowType=Discovery`, `CurrentStage=UR`, reutilizando `CreateProjectWithInitialDevFlowRunAsync`
- [ ] AC2: La descripción del proyecto queda como input inicial del run (Title/Description)
- [ ] AC3: Se respeta el límite de proyecto activo existente (regla actual hardcodeada; se migrará a `IPlanLimitService` en Hito 14)
- [ ] AC4: La respuesta incluye `RunId` para que la UI navegue al Discovery
- [ ] AC5: Tests de integración cubren creación y límite

#### Tarea 10.1.1 – Exponer creación proyecto + run Discovery

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/project-create-discovery-run` |
| **Área** | Backend |
| **Archivos** | `src/Gateway.Api/Program.cs`, `src/Gateway.Api/Services/ProjectService.cs`, DTOs de proyecto, tests `tests/MultiAgentSystem.Tests/Gateway/` |

**Pasos concretos**

1. Revisar `CreateProjectWithInitialDevFlowRunAsync` (ya existe) y ajustar lo mínimo (FlowType=Discovery explícito)
2. Cambiar `POST /api/projects` para usar ese método (o agregar `POST /api/projects/with-discovery` si se prefiere no alterar el contrato actual; decidir en la rama y documentar)
3. Devolver `ProjectId` + `RunId`
4. Tests: creación feliz, límite de proyecto activo, vínculo run-proyecto

**Decisión tomada en la rama (10.1.1):** se agregó el endpoint dedicado `POST /api/projects/with-discovery` en lugar de modificar `POST /api/projects`. Motivos: (a) el endpoint actual responde `ProjectDto` plano y es consumido por `UserProjectsView.razor` y por `GatewayTestHelpers.CreateProjectAsync` de toda la suite DevFlow; (b) el límite de "un proyecto activo" rompería esos tests, que crean varios proyectos con el mismo usuario contra una BD persistente. La UI de usuario migrará a este endpoint en la tarea 13.1.1; el endpoint legado se evaluará para deprecación al cierre del Hito 13. Respuesta: `ProjectWithDevFlowDto { project, initialRun }` (409 si hay proyecto activo, 400 sin nombre).

**Done:** Crear proyecto desde Swagger genera run Discovery consultable.

---

### HU 10.2 – El dueño del proyecto accede a su run Discovery

**Como** dueño de un proyecto
**Quiero** consultar el estado y artefactos de mi run Discovery
**Para** seguir el avance sin ser SuperUsuario

#### Criterios de aceptación

- [ ] AC1: Política `ProjectOwnerOrSuperUser`: el dueño accede a `GET /api/devflow/runs/{id}` y a sus artefactos solo si el run pertenece a un proyecto suyo
- [ ] AC2: SuperUsuario conserva acceso total (sin regresión en `/admin/devflow`)
- [ ] AC3: Un usuario no puede ver runs de proyectos ajenos (403/404)
- [ ] AC4: Endpoints de administración (crear run arbitrario, ejecutar etapa manual) siguen siendo SuperUsuario-only
- [ ] AC5: Tests de autorización por rol y por ownership

#### Tarea 10.2.1 – Política de acceso por ownership

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/devflow-owner-policy` |
| **Área** | Backend / seguridad |
| **Archivos** | `src/Gateway.Api/Program.cs` (policies + endpoints), `DevFlowService.cs` (validación ownership), tests |

**Pasos concretos**

1. Definir validación de ownership (`run.Project.UserId == currentUserId`) en service o handler de autorización
2. Aplicar a GET run / GET lista filtrada por proyecto propio
3. Mantener `SuperUserOnlyPolicy` en endpoints de gestión
4. Tests: dueño OK, ajeno 403/404, admin OK

**Done:** Dueño consulta su run; ajenos bloqueados; admin sin cambios.

**Depende de:** 10.1.1 (para tener runs de usuario reales en pruebas; no bloqueante)

---

## Hito 11 – Etapa UR conversacional (corazón del Discovery)

**Objetivo del hito:** Convertir la etapa UR de one-shot a conversación con estado dentro del mismo run: UR pregunta, el usuario responde, UR decide cuándo tiene la información completa y cierra la etapa generando su artefacto.

**Resultado esperado del hito:** Diálogo UR↔usuario persistido por run/etapa; al cierre, `DevFlowArtifact` de UR idéntico en contrato al actual; el resto del pipeline no nota la diferencia.

---

### HU 11.1 – Modelo de conversación por etapa

**Como** sistema
**Quiero** persistir los mensajes de una etapa conversacional asociados al run
**Para** dar contexto al agente y trazabilidad al discovery

#### Criterios de aceptación

- [ ] AC1: Entidad `DevFlowStageMessage` (`Id`, `DevFlowRunId`, `Stage`, `Sender` [User/Agent], `Content`, `CreatedAt`)
- [ ] AC2: Migración EF aplicable sobre la BD actual sin afectar tablas existentes
- [ ] AC3: Relación con `DevFlowRun` con borrado en cascada
- [ ] AC4: La entidad es genérica por etapa (no exclusiva de UR ni de Discovery) — requisito Etapa 2

#### Tarea 11.1.1 – Entidad DevFlowStageMessage + migración

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/stage-message-entity` |
| **Área** | Backend / datos |
| **Archivos** | `src/Gateway.Api/Data/Models/DevFlowStageMessage.cs` (nuevo), `ApplicationDbContext.cs`, migración nueva, tests de modelo |

**Pasos concretos**

1. Crear entidad + enum `StageMessageSender { User, Agent }`
2. `DbSet<DevFlowStageMessage>` + configuración (índice por `DevFlowRunId, Stage, CreatedAt`)
3. `dotnet ef migrations add AddDevFlowStageMessages`
4. Test de persistencia básico

**Done:** Migración aplicada en entorno local/Docker; tabla disponible.

---

### HU 11.2 – Endpoints de conversación UR

**Como** dueño del proyecto
**Quiero** enviar mensajes al agente UR y recibir sus respuestas dentro de mi run Discovery
**Para** completar el relevamiento de información

#### Criterios de aceptación

- [ ] AC1: `POST /api/devflow/runs/{id}/stage-messages` guarda el mensaje del usuario, invoca a `UrAgent` vía el `DevFlowAgentDispatcher` existente con historial completo de la conversación + descripción del proyecto, y guarda/devuelve la respuesta del agente
- [ ] AC2: `GET /api/devflow/runs/{id}/stage-messages` devuelve el historial ordenado
- [ ] AC3: Solo disponible si `CurrentStage` es conversacional (UR en Etapa 1) y el run está activo
- [ ] AC4: Autorización por ownership (HU 10.2)
- [ ] AC5: Errores de LLM devuelven error claro (reutilizar manejo existente), sin persistir respuesta vacía
- [ ] AC6: Tests de integración con agente mockeado

#### Tarea 11.2.1 – Endpoints de mensajes de etapa

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/ur-conversation-endpoints` |
| **Área** | Backend |
| **Archivos** | `Program.cs`, `DevFlowService.cs` (o `DevFlowConversationService` delgado que reutilice dispatcher), DTOs nuevos, tests |

**Pasos concretos**

1. Método `SendStageMessageAsync(runId, userId, content)`: valida estado/ownership → persiste mensaje User → construye contexto (descripción + historial) → dispatcher UR → persiste mensaje Agent → devuelve ambos
2. Método `GetStageMessagesAsync(runId, userId)`
3. Endpoints minimal API con la policy de ownership
4. Tests: flujo feliz, run en etapa no conversacional (409), ajeno (403)

**Done:** Conversación UR funcional vía Swagger con historial persistido.

**Depende de:** 11.1.1, 10.2.1

---

### HU 11.3 – Cierre de la etapa UR y generación del artefacto

**Como** agente UR
**Quiero** señalar cuándo el relevamiento está completo y consolidarlo en mi artefacto
**Para** que el pipeline continúe con PM/PO/UX/PLAN sin cambios

#### Criterios de aceptación

- [ ] AC1: Mecanismo de cierre definido: UR emite marcador de completitud en su respuesta (ej. bloque `[DISCOVERY_COMPLETO]`) **y/o** el usuario confirma con endpoint explícito `POST /api/devflow/runs/{id}/close-stage`
- [ ] AC2: Al cerrar, se genera el `DevFlowArtifact` de UR (resumen consolidado del relevamiento) reutilizando `ExecuteStageAsync`/dispatcher — mismo contrato actual
- [ ] AC3: El run avanza de etapa exactamente igual que hoy (gates incluidos)
- [ ] AC4: No se puede cerrar sin al menos un intercambio de mensajes
- [ ] AC5: Tests del cierre y la transición

#### Tarea 11.3.1 – Cierre de etapa conversacional

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/ur-conversation-close` |
| **Área** | Backend |
| **Archivos** | `DevFlowService.cs`, `Program.cs`, tests |

**Pasos concretos**

1. Definir regla de cierre (marcador del agente + confirmación del usuario; documentar la elegida en la rama)
2. Al cerrar: invocar generación del artefacto UR con la conversación como input (`InputText` = consolidado)
3. Reutilizar avance de etapa y creación de gate existentes sin modificarlos
4. Tests: cierre feliz, cierre sin mensajes (400), doble cierre (409)

**Done:** Etapa UR cerrable; artefacto UR persistido; run avanza a PM.

**Depende de:** 11.2.1

---

### HU 11.4 – Prompt UR en modo discovery conversacional

**Como** agente UR
**Quiero** instrucciones para conducir un product discovery por turnos
**Para** obtener del usuario toda la información necesaria antes de cerrar

#### Criterios de aceptación

- [ ] AC1: El prompt instruye: hacer preguntas de discovery (objetivo, usuarios, alcance, restricciones, prioridades), una tanda por turno, en español
- [ ] AC2: Define criterio de completitud y el marcador de cierre acordado en 11.3.1
- [ ] AC3: No rompe el uso actual de `UrAgent` en `/api/chat/run` ni en AutoDev (si hace falta, prompt parametrizado por modo/contexto, no un segundo agente)

#### Tarea 11.4.1 – Ajuste de prompt UR

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/ur-discovery-prompt` |
| **Área** | Prompts |
| **Archivos** | `src/Agents.UR/promptUsuarioRepresentante.md` (o prompt adicional inyectado por contexto desde el dispatcher) |

**Pasos concretos**

1. Decidir: prompt único con sección condicional vs. instrucción adicional inyectada en el contexto del dispatcher cuando la etapa es conversacional (preferida: inyección por contexto, no toca el prompt base)
2. Redactar instrucciones de discovery + marcador de cierre
3. Prueba manual con Ollama: 3+ turnos coherentes y cierre

**Done:** UR conversa con criterio de discovery y emite el marcador al completar.

**Depende de:** 11.3.1 (definición del marcador)

---

## Hito 12 – Etapas internas encadenadas y estimaciones

**Objetivo del hito:** Tras el cierre de UR, ejecutar PM→PO→UX→PLAN automáticamente reutilizando `ExecuteStageAsync`, dejando el run en `PendingApproval` con reportes y estimaciones para la decisión del cliente. Regla: si falta información, vuelve a UR (único interlocutor con el usuario).

**Resultado esperado del hito:** Discovery de punta a punta sin intervención de admin; reportes con estimaciones disponibles.

---

### HU 12.1 – Ejecución automática en cadena

**Como** sistema
**Quiero** ejecutar las etapas internas del Discovery en secuencia tras el cierre de UR
**Para** que el usuario no dependa de un operador

#### Criterios de aceptación

- [ ] AC1: Al cerrar UR, las etapas PM→PO→UX→PLAN se ejecutan en cadena reutilizando `ExecuteStageAsync` (sin segundo motor)
- [ ] AC2: Los gates internos se auto-aprueban en modo cadena (con registro de auditoría `ApprovedBy=system`); el comportamiento manual de admin queda intacto
- [ ] AC3: Al completar PLAN, el run queda `PendingApproval` (igual que hoy)
- [ ] AC4: Si una etapa falla (LLM), el run queda en estado consultable y reanudable; error visible
- [ ] AC5: AutoDev no cambia: la cadena automática solo aplica a runs Discovery iniciados por usuario (flag o convención; documentar)
- [ ] AC6: Tests de la cadena completa con mocks

#### Tarea 12.1.1 – Encadenador de etapas Discovery

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/auto-stage-chain` |
| **Área** | Backend |
| **Archivos** | `DevFlowService.cs` (método `RunDiscoveryChainAsync` que itera `GetStages` y llama `ExecuteStageAsync` + `ApproveGateAsync` existentes), `Program.cs`, tests |

**Pasos concretos**

1. Método que itera etapas restantes del pipeline Discovery llamando los métodos existentes (composición, no duplicación)
2. Auto-aprobación de gates internos con auditoría
3. Ejecución asíncrona (background) con estado consultable vía GET run; la UI sondea progreso
4. Manejo de fallo por etapa: log + run reanudable
5. Tests con dispatcher mockeado

**Done:** Cerrar UR dispara cadena; GET run muestra avance; final `PendingApproval`.

**Depende de:** 11.3.1

---

### HU 12.2 – Retorno a UR por información faltante

**Como** agente interno (PM/PO/UX/PLAN)
**Quiero** señalar información faltante para que UR la pida al usuario
**Para** respetar que UR es el único interlocutor

#### Criterios de aceptación

- [ ] AC1: Convención de marcador en la salida del agente (ej. `[INFO_FALTANTE: ...]`) detectada por la cadena
- [ ] AC2: Al detectarse, la cadena se pausa y el run vuelve a modo conversación UR con las preguntas pendientes visibles para el usuario
- [ ] AC3: Al cerrar UR nuevamente, la cadena se reanuda desde la etapa que faltaba (artefactos previos se conservan, nueva versión del artefacto UR)
- [ ] AC4: Tests del ciclo pausa-reanudación

#### Tarea 12.2.1 – Ciclo de información faltante

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/ur-missing-info-loop` |
| **Área** | Backend / prompts |
| **Archivos** | `DevFlowService.cs`, prompts de PM/PO/UX (instrucción del marcador), tests |

**Pasos concretos**

1. Definir marcador y añadir instrucción mínima a los prompts internos
2. Detección en la cadena → pausar → reabrir conversación UR con contexto de lo faltante
3. Reanudación desde etapa pendiente usando `Version` de artefactos existente
4. Tests: agente que marca faltante → pausa → respuesta usuario → reanudación

**Done:** Ciclo completo demostrado con mocks y manual con Ollama.

**Depende de:** 12.1.1

---

### HU 12.3 – Estimaciones estructuradas en PLAN

**Como** cliente
**Quiero** ver estimaciones (alcance, esfuerzo relativo, tiempos orientativos) en el reporte final
**Para** decidir si continúo con el desarrollo

#### Criterios de aceptación

- [ ] AC1: El prompt de la etapa PLAN exige una sección `## Estimaciones` con formato fijo (tabla: módulo, complejidad, esfuerzo relativo, dependencias)
- [ ] AC2: El DTO de detalle expone la sección de estimaciones identificable por la UI (parseo simple del `PayloadJson`; sin migración de BD)
- [ ] AC3: Si el LLM no genera la sección, el reporte se muestra igual (degradación elegante)
- [ ] AC4: Test del parseo

#### Tarea 12.3.1 – Estimaciones en prompt PLAN + exposición en DTO

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/plan-estimates` |
| **Área** | Prompts / backend |
| **Archivos** | Prompt PM/PLAN (instrucción por contexto en dispatcher), `DevFlowRunDetailResponse.cs` o DTO nuevo ligero, `DevFlowService.cs`, tests |

**Done:** GET run de un Discovery completo expone estimaciones identificables.

**Depende de:** 12.1.1 (no bloqueante)

---

## Hito 13 – UI del usuario final y aprobación del cliente

**Objetivo del hito:** Página de Discovery para el dueño del proyecto: chat con UR, progreso del pipeline, reportes con estimaciones y decisión final (aprobar/rechazar). Reutiliza componentes ya corregidos de `AdminDevFlowDetail`.

**Resultado esperado del hito:** El usuario completa el Discovery de punta a punta desde Blazor sin intervención de admin.

---

### HU 13.1 – Página Discovery del proyecto

**Como** dueño del proyecto
**Quiero** una pantalla con el chat de relevamiento y el progreso de las etapas
**Para** completar mi discovery

#### Criterios de aceptación

- [ ] AC1: Ruta `/project/{id}/discovery` accesible para el dueño (y SuperUsuario)
- [ ] AC2: Chat UR: historial, envío de mensajes, indicador de espera (reutilizar `ChatThinkingAnimation`), botón de confirmación de cierre cuando UR lo señala
- [ ] AC3: Pipeline visual Discovery (UR→PM→PO→UX→PLAN) reutilizando el componente dinámico por FlowType ya implementado en admin (extraer a componente compartido)
- [ ] AC4: Durante la cadena automática, progreso visible (polling al GET run)
- [ ] AC5: `Project.razor` actual no se elimina; la nueva página convive (navegación desde el dashboard del usuario)

#### Tarea 13.1.1 – Página de Discovery con chat UR

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/ui-user-discovery-page` |
| **Área** | UI Blazor |
| **Archivos** | `src/Gateway.Blazor/Pages/ProjectDiscovery.razor` (nuevo), componentes compartidos extraídos de `AdminDevFlowDetail.razor`, `DevFlowRunModels.cs`, `NavMenu`/dashboard |

**Depende de:** 11.2.1, 10.2.1

#### Tarea 13.1.2 – Componente compartido de pipeline/artefactos

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/ui-shared-pipeline-component` |
| **Área** | UI Blazor |
| **Archivos** | `src/Gateway.Blazor/Shared/DevFlowPipelineView.razor` (nuevo, extraído de admin), `AdminDevFlowDetail.razor` (consume el componente) |

**Pasos concretos:** extraer sin cambiar comportamiento admin; verificar `/admin/devflow/{id}` sin regresión.

**Done:** Admin y usuario comparten el mismo componente de pipeline.

---

### HU 13.2 – Reportes y estimaciones para el cliente

**Como** cliente
**Quiero** leer los reportes del discovery con sus estimaciones
**Para** tomar la decisión de continuar

#### Criterios de aceptación

- [ ] AC1: Vista de reportes por etapa (PayloadJson renderizado legible, markdown si aplica)
- [ ] AC2: Sección de estimaciones destacada (de 12.3.1)
- [ ] AC3: Solo lectura para el dueño (no puede reejecutar etapas)

#### Tarea 13.2.1 – Vista de reportes del Discovery

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/ui-discovery-reports` |
| **Área** | UI Blazor |
| **Archivos** | `ProjectDiscovery.razor`, componente de render de reporte |

**Depende de:** 13.1.1, 12.3.1

---

### HU 13.3 – Decisión del cliente (aprobación final)

**Como** cliente
**Quiero** aprobar o rechazar el resultado del discovery
**Para** decidir si el proyecto continúa a desarrollo

#### Criterios de aceptación

- [ ] AC1: Con run en `PendingApproval`, el dueño ve botones Aprobar / Rechazar
- [ ] AC2: Aprobar usa el endpoint de aprobación existente (con policy de ownership extendida al gate final del propio run)
- [ ] AC3: Rechazar cancela el run (comportamiento existente) con confirmación
- [ ] AC4: Tras aprobar, se muestra el estado "Discovery aprobado – Desarrollo disponible según plan" (conecta con Hito 14)
- [ ] AC5: Tests de autorización: dueño aprueba su gate final; no aprueba gates internos ni runs ajenos

#### Tarea 13.3.1 – Aprobación final por el dueño

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/client-final-approval` |
| **Área** | Backend + UI |
| **Archivos** | `Program.cs` (policy en endpoint approve), `DevFlowService.cs`, `ProjectDiscovery.razor`, tests |

**Depende de:** 13.1.1, 10.2.1

---

## Hito 14 – Gate por plan (stub) y validación end-to-end

**Objetivo del hito:** Introducir el punto de extensión del plan del usuario para continuar a Development (Etapa 2) e integrar el límite de proyectos activos existente; cerrar Etapa 1 con pruebas automatizadas y checklist manual.

---

### HU 14.1 – IPlanLimitService mínimo

**Como** sistema
**Quiero** centralizar las reglas de plan del usuario en un servicio
**Para** habilitar la Etapa 2 sin tocar el flujo

#### Criterios de aceptación

- [ ] AC1: `IPlanLimitService` con métodos mínimos: `CanCreateActiveProjectAsync(userId)` y `CanStartDevelopmentAsync(userId, projectId)` — firmas alineadas al diseño de `DISENO_VALIDACION_LIMITE_PROYECTOS_ACTIVOS.md`
- [ ] AC2: Implementación inicial: límite de proyecto activo actual (migrado desde `ProjectService`) + `CanStartDevelopment` configurable (por defecto: deshabilitado, mensaje "disponible según plan")
- [ ] AC3: `ProjectService` consume el servicio (se elimina la regla hardcodeada)
- [ ] AC4: AppAdmin **no** bypassa límites (decisión de diseño ya tomada)
- [ ] AC5: Tests unitarios del servicio

#### Tarea 14.1.1 – IPlanLimitService + integración

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/plan-limit-service` |
| **Área** | Backend |
| **Archivos** | `src/Gateway.Api/Services/IPlanLimitService.cs` + impl (nuevos), `ProjectService.cs`, `Program.cs` (DI), tests |

**Done:** Regla de límite centralizada; punto `CanStartDevelopment` listo para Etapa 2.

---

### HU 14.2 – Botón "Continuar a Desarrollo" (preparado, no activo)

**Como** cliente con discovery aprobado
**Quiero** ver la opción de continuar a desarrollo según mi plan
**Para** conocer el siguiente paso del producto

#### Criterios de aceptación

- [ ] AC1: Tras aprobación final, la UI muestra "Continuar a Desarrollo" consultando `CanStartDevelopmentAsync`
- [ ] AC2: En Etapa 1 el resultado por defecto es no disponible → botón deshabilitado con mensaje claro
- [ ] AC3: El endpoint que en Etapa 2 creará el run Development queda definido (puede devolver 501/feature-flag), documentado para no rehacer
- [ ] AC4: AutoDev/Development intactos: ningún cambio en sus pipelines

#### Tarea 14.2.1 – Punto de continuación a Development

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/continue-development-stub` |
| **Área** | Backend + UI |
| **Archivos** | `Program.cs`, `ProjectDiscovery.razor`, tests |

**Depende de:** 14.1.1, 13.3.1

---

### HU 14.3 – Tests automatizados y validación manual

#### Criterios de aceptación

- [ ] AC1: Test integración Discovery conversacional completo con mocks: crear proyecto → conversación UR → cierre → cadena → `PendingApproval` → aprobación dueño
- [ ] AC2: Tests de regresión AutoDev verdes sin modificación (prueba de preservación)
- [ ] AC3: Checklist manual E2E con Ollama real ejecutada y registrada

#### Tarea 14.3.1 – Tests E2E Discovery conversacional

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/discovery-mvp/tests-discovery-conversational` |
| **Área** | Tests |
| **Archivos** | `tests/MultiAgentSystem.Tests/Gateway/` (nuevos + `GatewayTestHelpers.cs`) |

#### Tareas manuales (sin rama)

| ID | Tarea |
|----|-------|
| 14.3.2 | E2E manual con Ollama: proyecto nuevo → discovery completo → aprobación |
| 14.3.3 | Regresión manual AutoDev: crear run AutoDev admin → UR→DEV sin cambios |
| 14.3.4 | Registrar evidencia (ids de run, capturas) en ticket de cierre |

---

## Resumen de ramas (orden de merge sugerido)

| Orden | Rama | Historia | Tarea |
|------:|------|----------|-------|
| 1 | `feature/discovery-mvp/plan-document` | — | Este documento |
| 2 | `feature/discovery-mvp/project-create-discovery-run` | HU 10.1 | 10.1.1 |
| 3 | `feature/discovery-mvp/devflow-owner-policy` | HU 10.2 | 10.2.1 |
| 4 | `feature/discovery-mvp/stage-message-entity` | HU 11.1 | 11.1.1 |
| 5 | `feature/discovery-mvp/ur-conversation-endpoints` | HU 11.2 | 11.2.1 |
| 6 | `feature/discovery-mvp/ur-conversation-close` | HU 11.3 | 11.3.1 |
| 7 | `feature/discovery-mvp/ur-discovery-prompt` | HU 11.4 | 11.4.1 |
| 8 | `feature/discovery-mvp/auto-stage-chain` | HU 12.1 | 12.1.1 |
| 9 | `feature/discovery-mvp/ur-missing-info-loop` | HU 12.2 | 12.2.1 |
| 10 | `feature/discovery-mvp/plan-estimates` | HU 12.3 | 12.3.1 |
| 11 | `feature/discovery-mvp/ui-shared-pipeline-component` | HU 13.1 | 13.1.2 |
| 12 | `feature/discovery-mvp/ui-user-discovery-page` | HU 13.1 | 13.1.1 |
| 13 | `feature/discovery-mvp/ui-discovery-reports` | HU 13.2 | 13.2.1 |
| 14 | `feature/discovery-mvp/client-final-approval` | HU 13.3 | 13.3.1 |
| 15 | `feature/discovery-mvp/plan-limit-service` | HU 14.1 | 14.1.1 |
| 16 | `feature/discovery-mvp/continue-development-stub` | HU 14.2 | 14.2.1 |
| 17 | `feature/discovery-mvp/tests-discovery-conversational` | HU 14.3 | 14.3.1 |

---

## Dependencias entre tareas

```
[10.1.1 crear proyecto+run] ──► [10.2.1 ownership policy]
                                      │
[11.1.1 entidad mensajes] ──► [11.2.1 endpoints conversación] ──► [11.3.1 cierre etapa] ──► [11.4.1 prompt UR]
                                                                        │
                                                  [12.1.1 cadena automática] ──► [12.2.1 loop info faltante]
                                                            │
                                                  [12.3.1 estimaciones]
                                                            │
[13.1.2 componente compartido] ──► [13.1.1 página discovery] ──► [13.2.1 reportes] ──► [13.3.1 aprobación cliente]
                                                                                              │
                                            [14.1.1 IPlanLimitService] ──► [14.2.1 continuar a development]
                                                                                              │
                                                                        [14.3.1 tests E2E] ──► (14.3.x manual)
```

```
Hito 10 ──► Hito 11 ──► Hito 12 ──► Hito 13 ──► Hito 14
acceso      conversación  cadena+     UI usuario   plan gate +
usuario     UR            estimación  +aprobación  validación
```

---

## Checklist de merge (integrador)

Por cada rama mergeada a `main`:

- [ ] `dotnet build` sin errores
- [ ] `dotnet test` relevantes verdes
- [ ] Alcance limitado a la tarea de la rama
- [ ] **Smoke de preservación AutoDev:** crear run AutoDev + ejecutar UR desde admin
- [ ] Si aplica UI: verificar `/admin/devflow/{id}` (sin regresión) y página de usuario
- [ ] La IA no commitea ni pushea: mensaje de commit sugerido en la entrega de cada rama (GitRules)

---

## Referencias

- Visión del producto: este documento, sección inicial (directiva de junio 2026)
- [`GitRules.md`](../GitRules.md) – protocolo git obligatorio
- [`PLAN_DEVFLOW_AGENT_RESPONSES.md`](./PLAN_DEVFLOW_AGENT_RESPONSES.md) – plan previo (hitos 6–9, correcciones de tracing ya validadas E2E)
- [`../DISENO_VALIDACION_LIMITE_PROYECTOS_ACTIVOS.md`](../DISENO_VALIDACION_LIMITE_PROYECTOS_ACTIVOS.md) – diseño de límites por plan
- [`../DISENO_MODULO_SUSCRIPCIONES.md`](../DISENO_MODULO_SUSCRIPCIONES.md) – diseño de suscripciones (Etapa 2)
- Código clave: `DevFlowService`, `DevFlowPipeline`, `DevFlowAgentDispatcher`, `ProjectService`, `AdminDevFlowDetail.razor`
