# Plan de implementación – Próximos hitos del proyecto

**Fecha:** Febrero 2025  
**Objetivo:** Convertir los próximos pasos en hitos, historias de usuario y tareas técnicas ejecutables.

---

# 1️⃣ Integración de RAG

## Hito
Incorporar RAG (Retrieval-Augmented Generation) al flujo de agentes para que utilicen conocimiento almacenado en la base vectorial (pgvector) y mejoren la calidad de las respuestas.

---

### Historia de usuario 1.1 – Infraestructura pgvector

**Como** administrador de infraestructura  
**Quiero** que PostgreSQL tenga la extensión pgvector instalada y operativa  
**Para** permitir búsqueda vectorial y almacenar embeddings

#### Tareas técnicas
- [ ] Sustituir imagen `postgres:16` por `pgvector/pgvector:pg16` en `infra/docker-compose.yml`
- [ ] Documentar en README que el volumen `pgdata` puede requerir recreación si ya existía
- [ ] Verificar que la extensión `vector` se crea correctamente al iniciar el contenedor
- [ ] Añadir variables de entorno opcionales para dimensiones de embedding si aplica

**Afecta:** infraestructura, base de datos

#### Rama sugerida
`feature/rag/infra-pgvector`

---

### Historia de usuario 1.2 – Ingesta de documentos

**Como** administrador o usuario con permisos  
**Quiero** cargar documentos (texto o archivos) al sistema de conocimiento  
**Para** que los agentes puedan consultar información relevante en sus respuestas

#### Tareas técnicas
- [ ] Crear endpoint `POST /api/knowledge/ingest` que reciba texto o multipart (txt, md)
- [ ] Dividir el contenido en chunks (tamaño configurable, solapamiento opcional)
- [ ] Invocar `IKnowledgeStore.AddManyAsync` con los chunks generados
- [ ] Añadir validación de permisos (por ejemplo, Admin/SuperUsuario)
- [ ] Configurar opciones de chunking en `appsettings` (tamaño, overlap)
- [ ] Tests unitarios para el servicio de ingesta
- [ ] Tests de integración para el endpoint (con mock de embeddings)

**Afecta:** backend, base de datos

#### Rama sugerida
`feature/rag/document-ingestion`

---

### Historia de usuario 1.3 – UI para gestión de conocimiento

**Como** administrador  
**Quiero** una interfaz para subir y gestionar documentos de conocimiento  
**Para** administrar el conocimiento sin usar la API directamente

#### Tareas técnicas
- [ ] Crear página Blazor `/admin/knowledge` (protegida, rol Admin/SuperUsuario)
- [ ] Componente para subir archivos (txt, md) con preview
- [ ] Listar documentos ingeridos (si se añade metadata de origen)
- [ ] Botón para eliminar/regenerar embeddings de un documento (si el modelo lo permite)
- [ ] Mensajes de progreso durante la ingesta
- [ ] Enlazar en NavMenu bajo Administración

**Afecta:** frontend, backend (si se necesita endpoint de listado)

#### Rama sugerida
`feature/rag/knowledge-ui`

---

### Historia de usuario 1.4 – Inyección de contexto RAG en agentes

**Como** usuario final  
**Quiero** que los agentes utilicen información del conocimiento almacenado  
**Para** recibir respuestas más precisas y alineadas con la documentación del sistema

#### Tareas técnicas
- [ ] Inyectar `IRetriever` en el Orquestador (o en cada agente según diseño)
- [ ] Antes de cada llamada a agente, invocar `GetContextAsync(query, k)` usando el mensaje del usuario o contexto acumulado como query
- [ ] Añadir el contexto RAG al prompt de cada agente (ej. sección "Conocimiento relevante: {context}")
- [ ] Parámetro `k` configurable (appsettings o por agente)
- [ ] Manejar el caso en que no haya resultados (no romper el flujo)
- [ ] Modificar PmAgent, PoAgent, DevAgent, UxAgent, UrAgent para usar el contexto inyectado
- [ ] Actualizar tests de agentes con mock de IRetriever

**Afecta:** agentes, orquestador, Shared.Abstractions (si se amplía `AgentTurn`)

#### Rama sugerida
`feature/rag/context-injection`

---

### Historia de usuario 1.5 – Configuración de RAG por agente

**Como** administrador  
**Quiero** activar o desactivar RAG por agente y ajustar parámetros (k, peso)  
**Para** controlar el uso de conocimiento según el rol

#### Tareas técnicas
- [ ] Extender modelo `Behavior` o añadir entidad `AgentRagConfig` (AgentRole, Enabled, K, Weight)
- [ ] Migración EF para la nueva tabla/config
- [ ] Servicio o extensión de `IBehaviorProvider` que exponga config RAG por agente
- [ ] Leer config en el orquestador al construir el contexto
- [ ] UI en ConfigureAgent o Behaviors para configurar RAG por rol

**Afecta:** backend, base de datos, frontend, agentes

#### Rama sugerida
`feature/rag/agent-config`

---

# 2️⃣ Observabilidad

## Hito
Habilitar observabilidad completa del sistema mediante OpenTelemetry, trazado distribuido y métricas para analizar rendimiento, errores y uso de recursos.

---

### Historia de usuario 2.1 – OpenTelemetry con backend OTLP

**Como** desarrollador u operador  
**Quiero** que las trazas y métricas se exporten a un colector OTLP  
**Para** visualizarlas en Jaeger, Grafana u otro backend

#### Tareas técnicas
- [ ] Habilitar OpenTelemetry por defecto cuando `OtlpEndpoint` esté configurado
- [ ] Añadir servicio Jaeger (u OTLP Collector) al `docker-compose.yml`
- [ ] Exponer puerto 4317 (OTLP gRPC) o 4318 (OTLP HTTP) del colector
- [ ] Documentar en README cómo ejecutar con observabilidad
- [ ] Instrumentar trazas para `POST /api/chat/run` y llamadas a agentes
- [ ] Añadir spans personalizados en Orchestrator por cada agente

**Afecta:** infraestructura, backend, configuración

#### Rama sugerida
`feature/observability/opentelemetry-jaeger`

---

### Historia de usuario 2.2 – Métricas de agentes

**Como** administrador u operador  
**Quiero** ver métricas de uso por agente (latencia, tokens, errores)  
**Para** detectar cuellos de botella y optimizar costes

#### Tareas técnicas
- [ ] Registrar contadores de métricas: `agent.invocations`, `agent.errors`, `agent.duration`
- [ ] Añadir dimensiones por AgentRole y ConversationId (opcional, cuidar cardinalidad)
- [ ] Integrar con `ILlmClient` para estimar tokens (si el proveedor lo expone) o contar caracteres como proxy
- [ ] Crear endpoint `GET /api/admin/metrics` (opcional) que exponga resumen para dashboard
- [ ] Documentar las métricas expuestas

**Afecta:** backend, agentes, orquestador

#### Rama sugerida
`feature/observability/agent-metrics`

---

### Historia de usuario 2.3 – Dashboard de observabilidad

**Como** administrador  
**Quiero** un dashboard básico de métricas y estado del sistema  
**Para** supervisar el funcionamiento sin depender de Jaeger/Grafana externos

#### Tareas técnicas
- [ ] Crear página Blazor `/admin/observability` (protegida, Admin/SuperUsuario)
- [ ] Mostrar gráficos simples (latencia media, errores por agente, conversaciones/hora)
- [ ] Consumir `/api/admin/metrics` o Prometheus scrape si se expone
- [ ] Usar librería de gráficos (ej. Chart.js via JS interop o componente MudBlazor)
- [ ] Añadir enlace en NavMenu bajo Administración

**Afecta:** frontend, backend (endpoint de métricas)

#### Rama sugerida
`feature/observability/dashboard`

---

### Historia de usuario 2.4 – Logs estructurados y correlación

**Como** desarrollador u operador  
**Quiero** logs estructurados (JSON) con trace_id para correlacionar con trazas  
**Para** depurar incidencias más rápido

#### Tareas técnicas
- [ ] Configurar Serilog o equivalente para logs JSON en entorno no-Development
- [ ] Inyectar trace_id en el contexto de logging cuando haya Activity de OpenTelemetry
- [ ] Documentar formato de logs para ingest en ELK/Loki

**Afecta:** backend, configuración

#### Rama sugerida
`feature/observability/structured-logs`

---

# 3️⃣ Guardrails para agentes

## Hito
Implementar controles de seguridad, límites y validaciones para que el uso de los agentes sea predecible, seguro y acotado en coste.

---

### Historia de usuario 3.1 – Límite de tokens por request

**Como** administrador  
**Quiero** configurar un límite máximo de tokens (o caracteres) por conversación o por request  
**Para** controlar costes y evitar respuestas excesivamente largas

#### Tareas técnicas
- [ ] Añadir opción en `appsettings`: `AgentLimits:MaxTokensPerRequest`, `MaxTokensPerConversation`
- [ ] Crear servicio `ITokenEstimator` que estime tokens (caracteres/4 como aproximación o API específica)
- [ ] En el Orquestador, interrumpir el flujo si se supera el límite y devolver mensaje controlado
- [ ] Añadir contador de tokens estimados en el contexto de la conversación
- [ ] Tests para el límite de tokens

**Afecta:** backend, orquestador, configuración

#### Rama sugerida
`feature/guardrails/token-limits`

---

### Historia de usuario 3.2 – Rate limiting por usuario

**Como** administrador  
**Quiero** limitar el número de requests de chat por usuario en un intervalo de tiempo  
**Para** evitar abusos y asegurar equidad de uso

#### Tareas técnicas
- [ ] Añadir middleware o atributo de rate limiting (ej. AspNetCoreRateLimit o custom)
- [ ] Configurar límites por usuario autenticado (ej. 20 req/min)
- [ ] Devolver 429 Too Many Requests con headers Retry-After
- [ ] Configuración en appsettings por entorno
- [ ] Tests de integración para rate limit

**Afecta:** backend, configuración

#### Rama sugerida
`feature/guardrails/rate-limiting`

---

### Historia de usuario 3.3 – Filtrado de contenido sensible

**Como** administrador  
**Quiero** que el sistema detecte y rechace prompts que contengan contenido inapropiado o sensible  
**Para** cumplir políticas de uso responsable

#### Tareas técnicas
- [ ] Definir interfaz `IContentFilter` con método `IsAllowed(string text)`
- [ ] Implementación básica: lista de palabras bloqueadas o regex (configurable)
- [ ] Implementación opcional: llamada a API de moderación (OpenAI Moderation, Azure Content Safety)
- [ ] Invocar filtro antes de procesar el mensaje del usuario en `/api/chat/run`
- [ ] Devolver 400 con mensaje genérico si el contenido no pasa el filtro
- [ ] Configuración para activar/desactivar y elegir estrategia
- [ ] Tests para el filtro

**Afecta:** backend, configuración

#### Rama sugerida
`feature/guardrails/content-filter`

---

### Historia de usuario 3.4 – Timeout configurable por agente

**Como** administrador  
**Quiero** poder configurar timeouts por llamada a LLM o por flujo completo  
**Para** evitar bloqueos indefinidos si el proveedor LLM tarda demasiado

#### Tareas técnicas
- [ ] Añadir `AgentLimits:AgentTimeoutSeconds` y `AgentLimits:TotalFlowTimeoutSeconds` en appsettings
- [ ] Envolver llamadas a `agent.HandleAsync` y `llm.CompleteAsync` con `CancellationTokenSource` con timeout
- [ ] Capturar `OperationCanceledException` y devolver mensaje de error controlado al usuario
- [ ] Logging de timeouts para análisis
- [ ] Tests con mock que simule demora

**Afecta:** backend, orquestador, agentes

#### Rama sugerida
`feature/guardrails/timeouts`

---

### Historia de usuario 3.5 – UI para configurar guardrails

**Como** administrador  
**Quiero** una interfaz para ver y modificar los parámetros de guardrails  
**Para** ajustar límites sin editar archivos de configuración

#### Tareas técnicas
- [ ] Crear página Blazor `/admin/guardrails`
- [ ] Formulario para: límite de tokens, rate limit, activar/desactivar filtro de contenido, timeouts
- [ ] Persistir en `SystemConfigurations` o tabla específica si se prefiere separación
- [ ] Endpoint `GET/PUT /api/admin/guardrails` para leer y actualizar config
- [ ] Validación en frontend y backend

**Afecta:** frontend, backend, base de datos (opcional)

#### Rama sugerida
`feature/guardrails/config-ui`

---

# 4️⃣ Evolución hacia arquitectura distribuida

## Hito
Preparar el sistema para una arquitectura distribuida, desacoplando agentes mediante eventos o servicios independientes, sin realizar la migración completa todavía.

---

### Historia de usuario 4.1 – Abstracción de transporte para agentes

**Como** desarrollador  
**Quiero** que los agentes se invoquen mediante una abstracción (ILocalAgent vs IRemoteAgent)  
**Para** poder sustituir llamadas in-process por llamadas remotas sin cambiar el orquestador

#### Tareas técnicas
- [ ] Definir interfaz `IAgentInvoker` o ampliar `IAgent` con estrategia de invocación
- [ ] Implementación local actual: invocar directamente `HandleAsync`
- [ ] El Orquestador usa `IAgentInvoker` en lugar de `IEnumerable<IAgent>` directo
- [ ] Registrar implementación local en DI
- [ ] Refactorizar tests para usar la nueva abstracción
- [ ] Documentar el contrato para futuras implementaciones remotas

**Afecta:** Shared.Abstractions, Orchestrator.App, Gateway.Api (DI), agentes

#### Rama sugerida
`feature/distributed/agent-invoker-abstraction`

---

### Historia de usuario 4.2 – Definición de eventos de dominio

**Como** arquitecto de software  
**Quiero** definir un modelo de eventos para el flujo multi-agente  
**Para** preparar la futura comunicación asíncrona entre componentes

#### Tareas técnicas
- [ ] Crear proyecto o carpeta `Shared.Events` con contratos de eventos
- [ ] Definir eventos: `UserMessageReceived`, `AgentRequested`, `AgentResponseReceived`, `FlowCompleted`
- [ ] Incluir: ConversationId, AgentRole, Payload, Timestamp
- [ ] Documentar el esquema de eventos y flujo esperado
- [ ] Sin implementación de bus todavía; solo contratos y tipos

**Afecta:** nuevo módulo Shared.Events (o Shared.Abstractions)

#### Rama sugerida
`feature/distributed/event-contracts`

---

### Historia de usuario 4.3 – Integración de NATS como event bus

**Como** desarrollador u operador  
**Quiero** publicar eventos del flujo en NATS  
**Para** permitir que otros servicios (logging, analytics, réplicas) consuman los eventos

#### Tareas técnicas
- [ ] Añadir servicio `nats` al `docker-compose.yml` (puerto 4222)
- [ ] Añadir paquete NuGet para cliente NATS (.NET)
- [ ] Crear `IEventPublisher` con implementación NATS
- [ ] Publicar eventos en el Orquestador (UserMessageReceived, AgentResponseReceived, FlowCompleted) sin bloquear el flujo
- [ ] Configuración de URL de NATS en appsettings
- [ ] Documentar topics y formato de mensajes

**Afecta:** infraestructura, backend, orquestador

#### Rama sugerida
`feature/distributed/nats-event-bus`

---

### Historia de usuario 4.4 – Servicio de agente como API independiente (PoC)

**Como** desarrollador  
**Quiero** un microservicio mínimo que exponga un agente (ej. Dev) vía HTTP/gRPC  
**Para** validar la arquitectura distribuida antes de migrar todos los agentes

#### Tareas técnicas
- [ ] Crear proyecto `Agents.Dev.Api` (ASP.NET Minimal API)
- [ ] Endpoint `POST /invoke` que reciba `AgentTurn` y devuelva `ChatMessage`
- [ ] El servicio usa DevAgent, ILlmClient, IBehaviorProvider (o mock)
- [ ] Añadir al docker-compose con red compartida
- [ ] Implementación remota de `IAgentInvoker` en Gateway.Api que llame a este servicio
- [ ] Configuración para activar modo distribuido (feature flag)
- [ ] Documentar cómo ejecutar en modo distribuido

**Afecta:** infraestructura, nuevo proyecto, Gateway.Api

#### Rama sugerida
`feature/distributed/agent-microservice-poc`

---

### Historia de usuario 4.5 – Descubrimiento y resiliencia básica

**Como** operador  
**Quiero** que el Gateway pueda descubrir agentes remotos y manejar fallos  
**Para** que el sistema sea tolerante a fallos cuando los agentes sean servicios externos

#### Tareas técnicas
- [ ] Configurar URLs de agentes remotos en appsettings (por AgentRole)
- [ ] Implementar reintentos con backoff (Polly) en las llamadas HTTP a agentes remotos
- [ ] Circuit breaker opcional para evitar sobrecarga si un agente falla reiteradamente
- [ ] Fallback a agente local si el remoto no está disponible (configurable)
- [ ] Logging y métricas de fallos y reintentos

**Afecta:** backend, configuración

#### Rama sugerida
`feature/distributed/resilience`

---

# Resumen de ramas sugeridas

| Área | Rama | Prioridad sugerida |
|------|------|--------------------|
| RAG | `feature/rag/infra-pgvector` | 1 |
| RAG | `feature/rag/document-ingestion` | 2 |
| RAG | `feature/rag/context-injection` | 3 |
| RAG | `feature/rag/knowledge-ui` | 4 |
| RAG | `feature/rag/agent-config` | 5 |
| Observabilidad | `feature/observability/opentelemetry-jaeger` | 1 |
| Observabilidad | `feature/observability/agent-metrics` | 2 |
| Observabilidad | `feature/observability/dashboard` | 3 |
| Observabilidad | `feature/observability/structured-logs` | 4 |
| Guardrails | `feature/guardrails/token-limits` | 1 |
| Guardrails | `feature/guardrails/rate-limiting` | 2 |
| Guardrails | `feature/guardrails/content-filter` | 3 |
| Guardrails | `feature/guardrails/timeouts` | 4 |
| Guardrails | `feature/guardrails/config-ui` | 5 |
| Distribuido | `feature/distributed/agent-invoker-abstraction` | 1 |
| Distribuido | `feature/distributed/event-contracts` | 2 |
| Distribuido | `feature/distributed/nats-event-bus` | 3 |
| Distribuido | `feature/distributed/agent-microservice-poc` | 4 |
| Distribuido | `feature/distributed/resilience` | 5 |

---

# Dependencias entre hitos

```
RAG infra (1.1) ──► RAG ingesta (1.2) ──► RAG UI (1.3)
        │
        └──► RAG context injection (1.4) ──► RAG agent config (1.5)

Observability 2.1 ──► 2.2 ──► 2.3
        │
        └──► 2.4

Guardrails: 3.1, 3.2, 3.3, 3.4 pueden ir en paralelo; 3.5 depende de tener varios implementados

Distribuido: 4.1 ──► 4.2 ──► 4.3
        └──► 4.4 (puede hacerse tras 4.1)
        └──► 4.5 (tras 4.4)
```

---

*Documento de planificación. No incluye implementación de código.*
