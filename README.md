# Sistema Multi-Agente

Sistema multi-agente donde varios modelos (agentes) asumen roles distintos y colaboran para resolver tareas de desarrollo.

## Arquitectura

El sistema está compuesto por:

- **Gateway.Api**: Minimal API que expone endpoints REST y WebSocket para comunicación con el frontend
- **Orchestrator.App**: Núcleo que coordina el flujo de agentes
- **Agents.PM**: Agente Project Manager que coordina tareas y define criterios de aceptación
- **Agents.Dev**: Agente Desarrollador que propone diseños técnicos y código
- **Shared.Abstractions**: Contratos, DTOs e interfaces compartidas
- **Shared.Knowledge**: Conectores a RAG (Vector DB + Blob) - pendiente de implementación

## Requisitos

- .NET 9.0 SDK
- Docker y Docker Compose (para ejecutar con Ollama)

## Configuración

1. Edita `src/Gateway.Api/appsettings.json` para configurar la conexión a tu proveedor de LLM:

```json
{
  "OpenAI": {
    "BaseUrl": "http://localhost:11434/v1",
    "Model": "llama3.2",
    "Key": ""
  }
}
```

Para usar OpenAI o Azure OpenAI, cambia `BaseUrl` y proporciona la `Key` correspondiente.

## Ejecución Local

### Opción 1: Con Docker Compose (incluye Ollama)

```bash
docker-compose -f infra/docker-compose.yml up -d
```

Luego, descarga un modelo en Ollama:
```bash
docker exec -it <ollama-container> ollama pull llama3.2
```

### Opción 2: Desarrollo Local

```bash
# Restaurar dependencias
dotnet restore

# Compilar solución
dotnet build

# Ejecutar Gateway.Api
cd src/Gateway.Api
dotnet run
```

El API estará disponible en `http://localhost:8093` (cuando se ejecuta con Docker) o `http://localhost:5077` (desarrollo local con Visual Studio).

## Interfaz web

- Abre `http://localhost:8093/` (Docker) o `http://localhost:5077/` (desarrollo local con Visual Studio) para usar la interfaz visual.
- Envía mensajes de texto de hasta 3000 caracteres y usa el dictado por voz (Chrome/Edge) para transcribir audio.
- Las respuestas del PM y del Dev se muestran en la conversación y pueden reproducirse con síntesis de voz del navegador.
- `test-websocket.html` sigue disponible para pruebas puntuales de WebSocket.

## Endpoints

### Swagger UI

- En desarrollo local (Visual Studio): `http://localhost:5077/swagger`
- En Docker (`docker-compose`): `http://localhost:8093/swagger`

Incluye documentación interactiva para:
- `POST /chat/run`
- `GET /chat/ws`

### POST /chat/run
Endpoint REST que ejecuta un flujo de agentes y devuelve todas las respuestas.

**Request:**
```json
{
  "conversationId": "c-123",
  "from": 0,
  "text": "Quiero un MVP para un to-do con Blazor y API REST.",
  "at": "2025-11-05T15:00:00Z"
}
```

**Response:**
```json
[
  {
    "conversationId": "c-123",
    "from": 1,
    "text": "Plan en 5 pasos...",
    "at": "2025-11-05T15:00:01Z"
  },
  {
    "conversationId": "c-123",
    "from": 3,
    "text": "Diseño de endpoints...",
    "at": "2025-11-05T15:00:02Z"
  }
]
```

### GET /chat/ws
Endpoint WebSocket para chat en tiempo real. Envía un `ChatMessage` JSON y recibe respuestas de los agentes en streaming.

## Pruebas automatizadas

```bash
dotnet test
```

Las pruebas cubren:
- Generación de prompts y respuestas en los agentes PM y Dev
- Flujo del orquestador asegurando el orden y encadenamiento del contexto
- Endpoint REST `/chat/run` validando que responde con la secuencia completa de agentes

## Flujo de Agentes

Por defecto, el sistema ejecuta el siguiente flujo:
1. **PM** (Project Manager): Analiza la solicitud y crea un plan
2. **Dev** (Desarrollador): Propone diseño técnico y código
3. **PM**: Valida y proporciona próximos pasos

Este flujo puede ser modificado en `Program.cs` del Gateway.Api.

## Próximos Pasos

- [ ] Implementar Shared.Knowledge con RAG (PostgreSQL + pgvector)
- [ ] Añadir agente UX/UI
- [ ] Integrar voz (STT/TTS)
- [ ] Añadir telemetría y observabilidad
- [ ] Implementar guardrails y políticas de seguridad
- [ ] Extraer agentes a microservicios (gRPC)

