# Multi Agente IA - Contexto del Proyecto

## Objetivo del producto

La plataforma no es inicialmente una fábrica automática de software. El MVP debe demostrar que puede transformar una idea o requerimiento difuso del usuario en un paquete tangible de análisis y planificación.

Flujo principal del MVP:

Requerimiento
↓
Análisis
↓
Descomposición
↓
Diseño
↓
Plan de implementación

El objetivo es que el cliente pueda evaluar si su idea tiene sentido, qué alcance tiene, qué riesgos implica, qué decisiones debe tomar y si conviene avanzar hacia un proyecto de desarrollo.

## Alcance del MVP

El MVP debe entregar como mínimo:

1. Análisis de la idea o requerimiento.
2. Descomposición funcional.
3. Documento funcional.
4. Diseño preliminar.
5. Plan de implementación.
6. Riesgos, supuestos y dependencias.
7. Recomendación sobre si conviene avanzar o no.

## Restricción clave sobre DEV

El agente DEV no debe intervenir en la etapa inicial de discovery.

DEV no debe modificar código existente de sistemas externos.

DEV solo puede crear código nuevo cuando:

- El proyecto fue creado dentro de la plataforma.
- La plataforma posee el contexto documental suficiente.
- El desarrollo fue aprobado luego de la etapa de análisis.
- El código nuevo se genera dentro de las carpetas propias del servidor/plataforma.

Si el cliente tiene un sistema externo existente, DEV no debe corregirlo ni modificarlo salvo que exista una futura funcionalidad formal de importación de proyecto/código fuente.

## Flujo conceptual

Idea del usuario
↓
UR: interpreta necesidad y genera requerimiento formal
↓
Analista de Negocio: analiza problema, valor, alcance y viabilidad
↓
PM: descompone trabajo, riesgos, dependencias y roadmap
↓
PO: define funcionalidades, reglas, criterios de aceptación y prioridades
↓
Arquitecto: propone enfoque técnico y restricciones
↓
Plan de Implementación: documento final para decidir si avanzar

## Estado deseado del MVP

El MVP se considera válido si permite que un usuario ingrese una idea y reciba un paquete documental suficientemente claro para tomar una decisión informada.

No se considera obligatorio generar código en este MVP.

## Arquitectura conocida

Backend: Gateway.Api en .NET
Frontend: Gateway.Blazor
Base de datos: PostgreSQL con pgvector
Autenticación: JWT + Identity
UI: Blazor
Agentes: UR, PM, PO, DEV, futuros agentes de arquitectura/QA
Prompts: configurables en base de datos mediante behaviors
Persistencia de flujo: DevFlowRun, DevFlowArtifact, DevFlowGate, BranchPlan
RAG: previsto, todavía no completamente integrado
Guardrails: previstos, todavía no completamente implementados

## Prioridad actual

Reordenar el sistema para soportar correctamente la etapa de discovery antes de avanzar hacia generación de código.