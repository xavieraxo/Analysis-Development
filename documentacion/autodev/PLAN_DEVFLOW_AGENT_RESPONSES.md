# PLAN DE EJECUCIÓN – Respuestas de agentes en DevFlow / Discovery

**Fecha:** Junio 2026  
**Origen:** Revisión técnica DevFlow (sin respuestas visibles / pipeline Discovery bloqueado / errores LLM opacos)  
**Objetivo:** Restaurar confianza end-to-end en la ejecución de agentes por etapa, sin mezclar `/api/chat/run` con DevFlow, sin RAG y sin romper AutoDev.

---

## Resumen ejecutivo

Este artefacto convierte el plan de corrección en **hitos ejecutables**. Cada hito agrupa **historias de usuario**; cada historia descompone **tareas técnicas**; **cada tarea se implementa en una rama git independiente** y se sube al remoto para merge manual a `main` / `master`.

**Reglas de trabajo**

| Regla | Detalle |
|-------|---------|
| Una tarea = una rama | No mezclar alcances de dos tareas en la misma rama |
| Merge manual | El integrador mergea cada rama cuando esté lista |
| Orden recomendado | Respetar dependencias del diagrama al final |
| Fuera de alcance | RAG, unificación con `/api/chat/run`, nuevas features de producto |
| Preservar | Pipeline AutoDev (`UR → PM → PO → DEV`), etapa DEV, mapping `PLAN → PM` |

**Convención de ramas**

```
feature/devflow-agents/<slug-corto>
```

**Artefacto machine-readable:** [`PLAN_DEVFLOW_AGENT_RESPONSES.branch-plan.json`](./PLAN_DEVFLOW_AGENT_RESPONSES.branch-plan.json)

---

## Hito 6 – Diagnóstico y baseline operativo

**Objetivo del hito:** Confirmar con evidencia si el backend invoca al LLM, persiste `PayloadJson` y dónde falla el flujo (IA, gates, UI).

**Resultado esperado del hito:** Runbook reproducible + baseline documentada (API, DB, Ollama, permisos) antes de tocar código de producto.

---

### HU 6.1 – Establecer baseline de diagnóstico DevFlow

**Como** responsable técnico del sistema  
**Quiero** un procedimiento repetible para validar ejecución de agentes en DevFlow  
**Para** distinguir fallos de configuración, backend y UI antes de implementar correcciones

#### Criterios de aceptación

- [ ] AC1: Existe checklist con pasos Swagger/Postman, consulta DB y revisión de logs
- [ ] AC2: El checklist distingue `/api/chat/run` vs `/api/devflow/runs`
- [ ] AC3: El checklist incluye verificación de Ollama (`ollama list`, modelo configurado)
- [ ] AC4: Queda registrado un ejemplo de run con artifact en DB (captura o query de referencia)

#### Tarea 6.1.1 – Runbook y checklist de diagnóstico

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/diagnostic-runbook` |
| **Área** | Documentación / operaciones |
| **Archivos** | `documentacion/autodev/DEVFLOW_AGENT_DIAGNOSTIC_RUNBOOK.md` (nuevo) |

**Pasos concretos**

1. Documentar secuencia: crear run → `execute-stage` UR → `GET run` → query SQL a `DevFlowArtifacts`
2. Documentar variables: `OpenAI__BaseUrl`, `OpenAI__Model`, `OpenAI__TimeoutSeconds`, `ApiBaseUrl` (Blazor)
3. Documentar comandos Docker: `docker logs infra-gateway-api-1`, `docker exec infra-ollama-1 ollama list`
4. Incluir matriz síntoma → causa probable → validación (sin respuesta UI vs 409 gate vs 500 LLM)
5. Añadir enlace desde este plan al runbook

**Tests esperados:** N/A (documentación). Validación manual por quien ejecute el hito.

**Done:** Runbook mergeado; al menos una ejecución manual registrada en el PR o en comentario de merge.

---

### HU 6.2 – Validación manual de conectividad IA (sin código)

**Como** operador del entorno  
**Quiero** verificar Ollama y configuración del API en mi entorno  
**Para** descartar que el problema sea solo infraestructura

#### Criterios de aceptación

- [ ] AC1: Ollama responde en la URL configurada
- [ ] AC2: El modelo de `OpenAI:Model` existe en el contenedor/host
- [ ] AC3: `POST /api/devflow/runs/{id}/execute-stage` probado manualmente al menos una vez
- [ ] AC4: Resultado anotado (200 + payload en DB, o error HTTP concreto)

#### Tareas (manuales – sin rama)

| ID | Tarea | Responsable |
|----|-------|-------------|
| 6.2.1 | Verificar `docker ps` / puertos 8094 (API), 11434 (Ollama), 8093 (Blazor) | Operador |
| 6.2.2 | Ejecutar `ollama list` y confirmar modelo (`llama3.2` o el configurado) | Operador |
| 6.2.3 | Probar `POST /execute-stage` con SuperUsuario vía Swagger | Operador |
| 6.2.4 | Consultar `SELECT "Stage", "AgentRole", LEFT("PayloadJson", 300) FROM "DevFlowArtifacts" ...` | Operador |
| 6.2.5 | Comparar con `POST /api/chat/run?projectId=` mismo usuario | Operador |

**Done:** Baseline anotada en issue/ticket antes de abrir ramas del Hito 7.

---

## Hito 7 – Corrección backend (observabilidad, errores, contrato API)

**Objetivo del hito:** Hacer visibles fallos del LLM, mejorar trazabilidad de ejecución y corregir detalles de orquestación en `DevFlowService` / dispatcher.

**Resultado esperado del hito:** Cada `execute-stage` o devuelve artifact persistido o error HTTP explicativo; logs suficientes para diagnosticar en minutos.

---

### HU 7.1 – Observabilidad en la ejecución de agentes DevFlow

**Como** desarrollador / operador  
**Quiero** logs estructurados al invocar agentes por etapa  
**Para** saber si el agente y el LLM fueron llamados y cuánto tardaron

#### Criterios de aceptación

- [ ] AC1: Log al inicio y fin de `DevFlowAgentDispatcher.ExecuteAsync` (runId, stage, role)
- [ ] AC2: Log de error con mensaje del LLM sin tragar excepción
- [ ] AC3: No se registra contenido completo del prompt en producción (solo metadatos / longitud)

#### Tarea 7.1.1 – Logging en DevFlowAgentDispatcher

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/dispatcher-logging` |
| **Archivos** | `src/Gateway.Api/Services/DevFlowAgentDispatcher.cs`, tests opcionales en `tests/.../DevFlowAgentDispatcherTests.cs` |

**Pasos concretos**

1. Inyectar `ILogger<DevFlowAgentDispatcher>`
2. Log Information: inicio/fin con `RunId`, `Stage`, `AgentRole`, duración ms
3. Log Error en catch/rethrow si se encapsula; preferir dejar propagar tras loguear
4. Test unitario mínimo con logger fake o verificación de no regresión

**Done:** Logs visibles en consola/API al ejecutar etapa.

---

### HU 7.2 – Errores de LLM traducidos en API DevFlow

**Como** consumidor de la API DevFlow  
**Quiero** respuestas HTTP claras cuando falla el proveedor IA  
**Para** no interpretar un 500 genérico como “agente silencioso”

#### Criterios de aceptación

- [ ] AC1: Fallo de `ILlmClient` devuelve JSON con `code` y `message` (ej. `LLM_UNAVAILABLE`)
- [ ] AC2: Código HTTP coherente (502/503 o 422 según estándar acordado)
- [ ] AC3: No se persiste artifact vacío cuando falla el LLM
- [ ] AC4: Test de integración simula LLM que lanza excepción

#### Tarea 7.2.1 – Manejo de errores LLM en DevFlowService

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/llm-error-handling` |
| **Archivos** | `DevFlowService.cs`, `Program.cs` (endpoint execute-stage), DTO/error helper, tests |

**Pasos concretos**

1. Capturar `HttpRequestException` (y/o wrapper de dominio) en `ExecuteStageAsync`
2. Mapear a `ExecuteStageResult` con status y payload `{ code, message, details? }`
3. Asegurar que no hay `SaveChanges` parcial si falla dispatcher
4. Test integración con `ILlmClient` fake que falla

**Done:** Swagger muestra error legible al detener Ollama.

**Depende de:** 7.1.1 (recomendado, no bloqueante)

---

### HU 7.3 – Contexto de artefactos previos alineado al pipeline

**Como** agente en etapa UX o PLAN  
**Quiero** recibir resumen de etapas anteriores según orden del flujo  
**Para** no depender del valor numérico crudo del enum

#### Criterios de aceptación

- [ ] AC1: `previousArtifactsSummary` usa orden de `IDevFlowPipeline.GetStages(flowType)`
- [ ] AC2: Discovery no incluye DEV en el resumen salvo artifact real
- [ ] AC3: Test unitario o integración cubre etapa UX en Discovery

#### Tarea 7.3.1 – Filtro de artefactos previos por pipeline

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/pipeline-artifacts-context` |
| **Archivos** | `DevFlowService.cs`, `tests/.../ExecuteDevFlowStageTests.cs` |

**Pasos concretos**

1. Reemplazar `(int)a.Stage < (int)stage` por índice en `GetStages(run.FlowType)`
2. Ordenar artifacts según pipeline, no solo enum
3. Añadir test Discovery hasta UX con verificación de contexto (mock agent captura input)

**Done:** Test verde; resumen coherente en etapas UX/PLAN.

---

### HU 7.4 – Contrato API de artifact en execute-stage

**Como** cliente UI o integrador  
**Quiero** recibir el contenido del artifact recién creado en la respuesta de execute-stage  
**Para** mostrar la respuesta del agente sin un GET adicional

#### Criterios de aceptación

- [ ] AC1: `ExecuteStageArtifactDto` incluye `PayloadJson`
- [ ] AC2: `ExecuteStageResponse` devuelve contenido no vacío tras ejecución exitosa
- [ ] AC3: DTO de detalle (`DevFlowArtifactSummaryDto`) mantiene `PayloadJson` (ya expuesto en GET)
- [ ] AC4: Comentarios XML alineados (ya no “sin PayloadJson”)

#### Tarea 7.4.1 – PayloadJson en ExecuteStageArtifactDto

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/execute-stage-payload-dto` |
| **Archivos** | `ExecuteStageResponse.cs`, `DevFlowRunModels.cs` (Blazor), `DevFlowService.cs`, tests GET/execute |

**Pasos concretos**

1. Añadir `PayloadJson` a DTOs API y Blazor
2. Mapear en `ExecuteStageAsync` al construir `ExecuteStageArtifactDto`
3. Actualizar tests que deserialicen respuesta

**Done:** POST execute-stage incluye texto del agente en `artifact.payloadJson`.

---

### HU 7.5 – Guardas de estado en runs Discovery completos

**Como** operador del flujo Discovery  
**Quiero** que un run en `PendingApproval` no reejecute la etapa terminal sin intención  
**Para** evitar artifacts duplicados en PLAN

#### Criterios de aceptación

- [ ] AC1: `ExecuteStageAsync` rechaza ejecución si status es `PendingApproval` y etapa ya tiene artifact (o según regla acordada)
- [ ] AC2: Mensaje 400 claro
- [ ] AC3: AutoDev no regresa (Completed sigue bloqueando)

#### Tarea 7.5.1 – Guard PendingApproval / etapa terminal

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/pending-approval-guard` |
| **Archivos** | `DevFlowService.cs`, tests |

**Pasos concretos**

1. Definir regla: si `PendingApproval` → no permitir nueva ejecución salvo override explícito futuro
2. Implementar validación antes de dispatcher
3. Tests AutoDev + Discovery

**Done:** Segunda ejecución PLAN en PendingApproval → 400.

---

## Hito 8 – Corrección UI DevFlow (visualización y Discovery)

**Objetivo del hito:** Mostrar respuestas reales de agentes y permitir completar Discovery desde Blazor (gates UX/PLAN).

**Resultado esperado del hito:** Operador ve contenido del artifact; aprueba gates Discovery; estados y pipeline coherentes con `FlowType`.

---

### HU 8.1 – Visualizar contenido de artifacts en detalle del run

**Como** SuperUsuario en panel DevFlow  
**Quiero** leer la respuesta del agente por etapa  
**Para** validar calidad antes de aprobar gates

#### Criterios de aceptación

- [ ] AC1: Tabla o panel muestra `PayloadJson` (expandible, modal o panel lateral)
- [ ] AC2: Tras ejecutar etapa, contenido visible sin recargar manualmente (usa `result.Run` o `artifact.payloadJson`)
- [ ] AC3: Texto largo con scroll; no rompe layout MudBlazor

#### Tarea 8.1.1 – UI contenido de artifacts

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/ui-artifact-content` |
| **Archivos** | `AdminDevFlowDetail.razor`, estilos mínimos si aplica |

**Pasos concretos**

1. Añadir columna “Contenido” o botón “Ver respuesta”
2. Modal reutilizable (similar a `BranchPlanCopyDialog`) con texto del artifact
3. Tras `ExecuteCurrentStage`, refrescar `_run` y opcionalmente abrir modal del artifact nuevo

**Done:** Usuario ve texto del agente en UI.

**Depende de:** 7.4.1 (recomendado para respuesta inmediata POST)

---

### HU 8.2 – Pipeline y gates dinámicos según FlowType

**Como** operador de runs Discovery  
**Quiero** ver y aprobar gates de UX y PLAN  
**Para** avanzar el flujo sin llamadas API manuales

#### Criterios de aceptación

- [ ] AC1: Pipeline visual usa etapas de Discovery cuando `FlowType == Discovery`
- [ ] AC2: Pestaña Approvals lista gates UR, PM, PO, UX, PLAN (Discovery) y UR..DEV (AutoDev)
- [ ] AC3: AutoDev sigue mostrando solo UR, PM, PO, DEV
- [ ] AC4: Labels UX, PLAN en `GetStageLabel`

#### Tarea 8.2.1 – Pipeline dinámico por FlowType

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/ui-dynamic-pipeline` |
| **Archivos** | `AdminDevFlowDetail.razor` |

**Pasos concretos**

1. Reemplazar `AllStages` fijo por método `GetStagesForRun(_run.FlowType)`
2. Espejar orden de `DevFlowPipeline` (constante local o helper compartido si existe en Blazor)
3. Actualizar `GetStageState`, loops de Approvals y pipeline visual

**Done:** Discovery muestra UR → PM → PO → UX → PLAN.

#### Tarea 8.2.2 – Gates Discovery UX/PLAN en Approvals

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/ui-discovery-gates` |
| **Archivos** | `AdminDevFlowDetail.razor` |

**Pasos concretos**

1. Asegurar tarjetas de aprobación para UX y PLAN
2. Mensaje 409 en UI: indicar gate pendiente con nombre de etapa legible
3. Prueba manual: ejecutar hasta UX, aprobar, ejecutar PLAN

**Done:** Discovery completable solo desde UI.

**Depende de:** 8.2.1 (puede mergearse en misma rama si se prefiere una sola PR; aquí separadas por foco)

---

### HU 8.3 – Metadatos de run y creación alineados a Discovery

**Como** SuperUsuario  
**Quiero** ver FlowType, PendingApproval y elegir tipo de flujo al crear run  
**Para** operar Discovery y AutoDev sin ambigüedad

#### Criterios de aceptación

- [ ] AC1: Detalle muestra `FlowType`
- [ ] AC2: `GetStatusLabel` incluye `PendingApproval`
- [ ] AC3: Diálogo crear run permite elegir Discovery / AutoDev / Development
- [ ] AC4: Listado AdminDevFlow muestra labels UX, PLAN en filtros

#### Tarea 8.3.1 – Labels FlowType y PendingApproval

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/ui-flowtype-status-labels` |
| **Archivos** | `AdminDevFlowDetail.razor`, `AdminDevFlow.razor` |

#### Tarea 8.3.2 – Selector FlowType al crear run

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/ui-create-flowtype` |
| **Archivos** | `CreateDevFlowRunDialog.razor` |

#### Tarea 8.3.3 – ApiBaseUrl en desarrollo local

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/ui-api-baseurl-config` |
| **Archivos** | `Gateway.Blazor/appsettings.Development.json` (ej. `http://localhost:8094`) |

**Done:** Blazor local apunta al API correcto; estados legibles.

---

## Hito 9 – Validación end-to-end y regresión

**Objetivo del hito:** Cerrar con pruebas automatizadas y checklist manual E2E; CI no sustituye prueba con Ollama real.

**Resultado esperado del hito:** AutoDev y Discovery verificados; regresión documentada.

---

### HU 9.1 – Tests automatizados Discovery y errores LLM

**Como** mantenedor del repositorio  
**Quiero** tests que cubran pipeline Discovery y fallo LLM  
**Para** evitar regresiones en gates y contrato API

#### Criterios de aceptación

- [ ] AC1: Test integración Discovery UR→…→PLAN con mocks (ya parcial en `ExecuteDevFlowStageTests`)
- [ ] AC2: Test error LLM devuelve código/mensaje estructurado
- [ ] AC3: Test payload en execute-stage response (tras 7.4.1)

#### Tarea 9.1.1 – Tests Discovery E2E (mock agents)

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/tests-discovery-e2e` |
| **Archivos** | `tests/MultiAgentSystem.Tests/Gateway/ExecuteDevFlowStageTests.cs`, helpers |

#### Tarea 9.1.2 – Tests respuesta error LLM

| Campo | Valor |
|-------|-------|
| **Rama** | `feature/devflow-agents/tests-llm-error-response` |
| **Archivos** | tests Gateway + fake LLM |

**Done:** `dotnet test` verde.

---

### HU 9.2 – Validación manual E2E con Ollama real

**Como** responsable de release  
**Quiero** una checklist manual con IA real  
**Para** certificar lo que CI con mocks no cubre

#### Tareas (manuales – sin rama)

| ID | Tarea |
|----|-------|
| 9.2.1 | AutoDev completo: UR → DEV, artifacts con texto, gates OK |
| 9.2.2 | Discovery completo: UR → PLAN, status `PendingApproval` |
| 9.2.3 | Detener Ollama: error visible en UI y API |
| 9.2.4 | Registrar evidencia (capturas / ids de run) en ticket de cierre |

**Done:** Checklist firmada; hito cerrado.

---

## Resumen de ramas (orden de merge sugerido)

| Orden | Rama | Historia | Tarea |
|------:|------|----------|-------|
| 1 | `feature/devflow-agents/diagnostic-runbook` | HU 6.1 | 6.1.1 |
| 2 | `feature/devflow-agents/dispatcher-logging` | HU 7.1 | 7.1.1 |
| 3 | `feature/devflow-agents/llm-error-handling` | HU 7.2 | 7.2.1 |
| 4 | `feature/devflow-agents/pipeline-artifacts-context` | HU 7.3 | 7.3.1 |
| 5 | `feature/devflow-agents/execute-stage-payload-dto` | HU 7.4 | 7.4.1 |
| 6 | `feature/devflow-agents/pending-approval-guard` | HU 7.5 | 7.5.1 |
| 7 | `feature/devflow-agents/ui-artifact-content` | HU 8.1 | 8.1.1 |
| 8 | `feature/devflow-agents/ui-dynamic-pipeline` | HU 8.2 | 8.2.1 |
| 9 | `feature/devflow-agents/ui-discovery-gates` | HU 8.2 | 8.2.2 |
| 10 | `feature/devflow-agents/ui-flowtype-status-labels` | HU 8.3 | 8.3.1 |
| 11 | `feature/devflow-agents/ui-create-flowtype` | HU 8.3 | 8.3.2 |
| 12 | `feature/devflow-agents/ui-api-baseurl-config` | HU 8.3 | 8.3.3 |
| 13 | `feature/devflow-agents/tests-discovery-e2e` | HU 9.1 | 9.1.1 |
| 14 | `feature/devflow-agents/tests-llm-error-response` | HU 9.1 | 9.1.2 |

**Nota:** Las tareas 6.2.x y 9.2.x son manuales; ejecutarlas entre hitos, sin rama obligatoria.

---

## Dependencias entre tareas

```
[6.1.1 runbook] ──► (baseline manual 6.2.x)

[7.1.1 logging] ──► [7.2.1 llm errors]

[7.4.1 payload DTO] ──► [8.1.1 UI content]

[8.2.1 dynamic pipeline] ──► [8.2.2 discovery gates]

[7.x + 8.x backend/UI] ──► [9.1.x tests] ──► (9.2.x manual E2E)
```

```
Hito 6 ──► Hito 7 ──► Hito 8 ──► Hito 9
 diagnóstico   backend      UI         validación
```

---

## Checklist de merge (integrador)

Por cada rama mergeada a `main` / `master`:

- [ ] `dotnet build` sin errores
- [ ] `dotnet test` relevantes verdes
- [ ] Alcance limitado a la tarea de la rama
- [ ] AutoDev smoke: crear run + ejecutar UR
- [ ] Si aplica UI: verificar pantalla `/admin/devflow/{id}`
- [ ] Eliminar rama remota opcional tras merge

---

## Referencias

- Revisión técnica origen (chat): diagnóstico DevFlow agent responses
- [`ROADMAP_AUTODEV_MVP.md`](./ROADMAP_AUTODEV_MVP.md) – convenciones hito / HU / rama
- Código clave: `DevFlowService`, `DevFlowAgentDispatcher`, `AdminDevFlowDetail.razor`, `OpenAiClient`
