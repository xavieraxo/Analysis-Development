# Diseño técnico: Módulo Deliverables

Documento de arquitectura para integrar el módulo Deliverables en la arquitectura actual sin romper Project → DevFlowRun → DevFlowArtifact. Sin implementación; solo diseño.

---

# 1. Resumen ejecutivo

**Qué es el módulo Deliverables**  
Es la capa de consolidación que expone el **resultado final consumible** de un flujo DevFlow. Hoy el sistema produce `DevFlowArtifact` por etapa (UR, PM, PO, DEV) y opcionalmente un `BranchPlan`; no existe un concepto explícito de “entregable final” que el usuario o el cliente pueda ver, descargar o exportar.

**Por qué hace falta**  
- Dar claridad al usuario sobre qué obtuvo al cerrar un proyecto o un run.  
- Diferenciar “evidencias parciales” (artifacts por etapa) del “entregable” (reporte, especificación, branch plan empaquetado, etc.).  
- Soportar distintos tipos de entrega (reporte, análisis, especificación, arquitectura, branch plan, paquete técnico, y en el futuro código/repo) sin acoplar todo a una sola estructura.  
- Mantener trazabilidad: qué artifacts y de qué run componen cada deliverable.  
- Respetar ownership, DevFlowScope (UserProject vs InternalSystem) y la regla de seguridad: deliverables de UserProject no pueden convertirse en modificaciones del sistema.

---

# 2. Arquitectura conceptual

**Cadena actual**  
`Project` → `DevFlowRun` → `DevFlowArtifact` (y opcionalmente `BranchPlan` por run).

**Cadena con Deliverables**  
`Project` → `DevFlowRun` → `DevFlowArtifact` → **Deliverable**

- **Project**: raíz de negocio; ownership Individual o Empresa; puede tener varios runs.  
- **DevFlowRun**: una ejecución/iteración del flujo; pertenece a un Project; tiene Scope (UserProject | InternalSystem).  
- **DevFlowArtifact**: salida cruda por etapa/agente (PayloadJson); evidencia parcial, no el “producto final”.  
- **Deliverable**: resultado final consolidado y consumible; se construye a partir de uno o más artifacts (y opcionalmente del BranchPlan) de un run; es lo que el usuario “recibe”.

**Relación conceptual**  
- Un **Deliverable** pertenece a un **DevFlowRun** (y por tanto a un **Project**).  
- Un Deliverable **referencia** los DevFlowArtifact (y opcionalmente el BranchPlan) que lo componen; no los reemplaza.  
- DevFlowArtifact sigue existiendo como evidencia por etapa; Deliverable es la vista consolidada “para entrega”.

---

# 3. Entidades recomendadas

## 3.1 Deliverable (núcleo del módulo)

**Propósito**  
Representar el resultado final consolidado de un run: un único “paquete” entregable (reporte, análisis, especificación, branch plan exportado, etc.) con tipo, estado, formato y contenido o referencia al contenido.

**Campos recomendados**

| Campo              | Tipo              | Descripción |
|--------------------|-------------------|-------------|
| Id                 | int               | PK. |
| DevFlowRunId       | int               | FK a DevFlowRun. Run que generó este entregable. |
| ProjectId          | int               | FK a Project (redundante pero útil para consultas y permisos). |
| Title              | string            | Título legible (ej. “Análisis UR + Especificación”). |
| DeliverableType    | enum              | Report, Analysis, FunctionalSpec, TechnicalArchitecture, BranchPlan, TechnicalPackage, CodeBootstrap, Other. |
| DeliverableStatus  | enum              | Draft, Finalized, Published, Archived. |
| DeliverableFormat  | enum              | Markdown, Json, Html, Pdf, Zip. |
| StorageType        | enum              | Inline (contenido en DB), BlobReference (ruta/URL), ExternalUrl. |
| ContentJson        | string?           | Si StorageType = Inline, contenido estructurado (ej. secciones) o texto. |
| ContentBlobPath    | string?           | Si StorageType = BlobReference, ruta o identificador del blob. |
| ExternalUrl        | string?           | Si StorageType = ExternalUrl. |
| Scope              | DevFlowScope      | Copia o derivado del run: UserProject o InternalSystem. |
| IsVisibleToClient  | bool              | Si el cliente/usuario final puede verlo (según tipo y rol). |
| Version            | int               | Versión del deliverable (1, 2, …). |
| CreatedAt          | DateTime          | UTC. |
| CreatedByUserId    | int?              | Quién lo creó/finalizó. |
| FinalizedAt        | DateTime?         | Cuándo pasó a Finalized. |

**Relaciones**  
- N:1 con **DevFlowRun** (un run puede tener varios deliverables).  
- N:1 con **Project** (vía DevFlowRun; ProjectId redundante para filtros y políticas).  
- Opcional: N:1 con **ApplicationUser** (CreatedByUser).

**MVP**  
Sí; es la entidad mínima obligatoria.

---

## 3.2 DeliverableArtifact (tabla puente / composición)

**Propósito**  
Registrar qué DevFlowArtifact (y en qué orden o rol) forman parte de un Deliverable. Permite trazabilidad y recomposición.

**Campos recomendados**

| Campo              | Tipo   | Descripción |
|--------------------|--------|-------------|
| Id                 | int    | PK. |
| DeliverableId      | int    | FK a Deliverable. |
| DevFlowArtifactId  | int    | FK a DevFlowArtifact. |
| SortOrder          | int    | Orden de inclusión (1, 2, …). |
| RoleInDeliverable  | string?| Rol conceptual (ej. "Context", "Spec", "Plan"). |

**Relaciones**  
- N:1 con Deliverable.  
- N:1 con DevFlowArtifact.  
- Unique: (DeliverableId, DevFlowArtifactId).

**MVP**  
Recomendado para MVP mínimo: sin esta tabla se puede guardar en Deliverable un array de IDs en JSON (snapshot); con ella la relación es normalizada y consultable.

**Fase posterior**  
Si en MVP se usa solo snapshot JSON en Deliverable, esta entidad se puede añadir en Fase 2.

---

## 3.3 DeliverableExport (opcional, fase posterior)

**Propósito**  
Registrar exportaciones generadas a partir de un Deliverable (PDF, ZIP, etc.) para auditoría y descargas sin regenerar cada vez.

**Campos sugeridos**  
Id, DeliverableId, ExportFormat, StoragePathOrUrl, GeneratedAt, GeneratedByUserId.

**MVP**  
No; fase de exportaciones (Fase 3).

---

## 3.4 DeliverableItem (opcional, fase posterior)

**Propósito**  
Descomponer un Deliverable en secciones/ítems (por ejemplo por etapa o por tipo de contenido) para UI, índice o exportación por partes.

**Campos sugeridos**  
Id, DeliverableId, Title, SectionType, ContentFragment, SortOrder.

**MVP**  
No; se puede modelar el contenido en ContentJson como estructura (lista de secciones) sin tabla propia. Entidad solo si más adelante se necesita consultar/versionar secciones por separado.

---

# 4. Enums recomendados

| Enum                 | Valores (sugeridos) | Uso |
|----------------------|---------------------|-----|
| **DeliverableType**  | Report, Analysis, FunctionalSpec, TechnicalArchitecture, BranchPlan, TechnicalPackage, CodeBootstrap, Other | Tipo de resultado entregable. |
| **DeliverableStatus**| Draft, Finalized, Published, Archived | Ciclo de vida del deliverable. |
| **DeliverableFormat**| Markdown, Json, Html, Pdf, Zip | Formato del contenido principal. |
| **DeliverableStorageType** | Inline, BlobReference, ExternalUrl | Dónde está almacenado el contenido. |

- **Report**: informe en texto/markdown.  
- **Analysis**: análisis (ej. UR).  
- **FunctionalSpec**: especificación funcional (PO).  
- **TechnicalArchitecture**: arquitectura técnica.  
- **BranchPlan**: export del BranchPlan del run.  
- **TechnicalPackage**: paquete que agrupa varios (ZIP de documentos).  
- **CodeBootstrap**: futuro repo o bootstrap de código.  
- **Draft**: en elaboración. **Finalized**: cerrado y listo para entrega. **Published**: visible/entregado al cliente. **Archived**: archivado.

---

# 5. Servicios recomendados

## 5.1 IDeliverableService

**Responsabilidad**  
CRUD y consulta de Deliverables; aplicación de reglas de visibilidad y ownership.

**Métodos principales**  
- `CreateDraftAsync(DevFlowRunId, DeliverableType, title, …)` – crea en estado Draft.  
- `GetByRunAsync(DevFlowRunId, userId/rol)` – lista deliverables del run (filtrados por permiso).  
- `GetByProjectAsync(ProjectId, userId/rol)` – lista deliverables del proyecto.  
- `GetByIdAsync(DeliverableId, userId/rol)` – detalle si tiene permiso.  
- `FinalizeAsync(DeliverableId, userId)` – pasa a Finalized (con validaciones).  
- `UpdateContentAsync(DeliverableId, content, userId)` – solo en Draft.

**Qué hace**  
- Valida que el run/project existan y que el usuario tenga acceso (ownership/rol).  
- Respeta Scope: InternalSystem solo SuperUsuario.  
- Marca IsVisibleToClient según tipo y política.

**Qué no hace**  
- No compone el contenido a partir de artifacts (eso es IDeliverableComposer).  
- No exporta a PDF/ZIP (eso es IDeliverableExportService si existe).

---

## 5.2 IDeliverableComposer

**Responsabilidad**  
Construir el contenido (ContentJson o estructura) de un Deliverable a partir de los DevFlowArtifact (y opcionalmente BranchPlan) del run.

**Métodos principales**  
- `ComposeFromRunAsync(DevFlowRunId, DeliverableType, options)` – genera contenido y opcionalmente crea/actualiza Deliverable en Draft.  
- `ComposeBranchPlanDeliverableAsync(DevFlowRunId)` – entregable que es el BranchPlan exportado.

**Qué hace**  
- Lee Artifacts del run (y BranchPlan si aplica).  
- Aplica plantilla/orden según DeliverableType.  
- Escribe en Deliverable.ContentJson (o prepara para BlobReference).  
- Puede crear filas en DeliverableArtifact para trazabilidad.

**Qué no hace**  
- No decide permisos (los asume ya validados por quien lo invoca).  
- No persiste el estado Finalized (lo hace IDeliverableService).

---

## 5.3 IDeliverableExportService (fase posterior)

**Responsabilidad**  
Exportar un Deliverable a PDF, HTML, ZIP, etc., y opcionalmente registrar en DeliverableExport.

**Métodos principales**  
- `ExportToFormatAsync(DeliverableId, format)` – devuelve stream o URL.  
- `GetOrCreateExportAsync(DeliverableId, format)` – reutiliza export previo si existe.

**MVP**  
No; se puede ofrecer solo “ver en pantalla” y “copiar Markdown/JSON” sin este servicio.

---

# 6. Reglas de negocio

1. **No todo proyecto termina en código**  
   El deliverable puede ser solo reporte, análisis o especificación; CodeBootstrap es un tipo futuro.

2. **Un proyecto puede tener múltiples deliverables**  
   Varios runs o varios entregables por run (ej. un Report y un BranchPlan).

3. **Un run puede generar más de un deliverable**  
   Por ejemplo: un deliverable tipo AnalysisReport y otro tipo BranchPlan para el mismo run.

4. **No todo deliverable es visible al cliente**  
   IsVisibleToClient depende de tipo, Scope y política; InternalSystem y borradores no son visibles al cliente final.

5. **Deliverables InternalSystem**  
   Solo visibles y gestionables por SuperUsuario; no se exponen a User/TenantAdmin/OperadorEmpresa/AppAdmin como entregables de negocio.

6. **Deliverables de UserProject no modifican el sistema**  
   Son solo datos de negocio; no hay ejecución automática ni aplicación sobre la plataforma; coherente con SECURITY.md.

7. **Finalize**  
   Solo se puede finalizar un deliverable en Draft si el run tiene los artifacts necesarios (según tipo); una vez Finalized, el contenido no se edita (solo nueva versión si se implementa versionado).

8. **Scope**  
   El Deliverable hereda el Scope del DevFlowRun del que proviene; no se puede asignar Scope distinto al del run.

---

# 7. Visibilidad por rol

| Rol              | UserProject (funcional/técnico)     | InternalSystem      |
|------------------|-------------------------------------|----------------------|
| **User**         | Ver/descargar deliverables de sus proyectos (ownership) que estén Finalized/Published y IsVisibleToClient. | No ve. |
| **OperadorEmpresa** | Ver deliverables de proyectos de la empresa según asignación. | No ve. |
| **TenantAdmin**  | Ver/exportar deliverables de proyectos del tenant. | No ve. |
| **AppAdmin**     | Ver todos los deliverables UserProject; no aplicar ni ver InternalSystem como “entregables”. | No ve (o solo lectura de metadato si se decide). |
| **SuperUsuario** | Ver/gestionar todo; publicar/archivar. | Ver, crear, finalizar; solo SuperUsuario puede “aplicar” algo al sistema. |

- **Funcional**: reportes, análisis, especificaciones.  
- **Técnico**: BranchPlan, TechnicalPackage, arquitectura.  
- **Internos**: deliverables con Scope InternalSystem; solo SuperUsuario.  
- **Visible al cliente**: IsVisibleToClient = true y estado adecuado.  
- **No visible al cliente**: borradores, internos, o tipos que la política marque como solo internos.

---

# 8. Diseño de UI

- **Pantalla del proyecto**  
  - Sección “Entregables” (o “Deliverables”) que lista los deliverables del proyecto (o del run principal si se usa MainDevFlowRunId).  
  - Tarjetas o filas: título, tipo, estado, fecha de finalización.  
  - Acciones: Ver, Descargar (según formato), “Ver branch plan” si aplica.

- **Vista del DevFlowRun**  
  - En el detalle del run, bloque “Entregables de este run”: lista de deliverables ligados a ese run.  
  - Botón “Generar entregable” (Draft) que dispara composición (IDeliverableComposer) según tipo elegido.  
  - Para cada deliverable: Ver, Finalizar (si Draft y con permiso), Descargar.

- **Cierre del flujo**  
  - Cuando el run pasa a Completed, se puede mostrar un resumen “Entregables generados” y enlaces a Ver/Descargar.  
  - Opción de “Crear entregable final” que compone (ej. Report + BranchPlan) y deja en Draft o Finalized según flujo.

- **Estados que ve el usuario**  
  - Draft, Finalized, Published (y Archived si se implementa).  
  - Solo entregables con permiso de lectura según rol y Scope.

- **Acciones**  
  - **Ver**: contenido en pantalla (Markdown renderizado, JSON formateado, etc.).  
  - **Descargar**: según StorageType/Format (archivo generado o enlace).  
  - **Exportar** (fase posterior): PDF, ZIP.  
  - **Revisar**: comentarios o aprobación si más adelante se agrega flujo de revisión.

---

# 9. Estrategia de implementación por fases

**Fase 1 – MVP mínimo**  
- Entidad **Deliverable** con campos esenciales: DevFlowRunId, ProjectId, Title, DeliverableType, DeliverableStatus, DeliverableFormat, StorageType = Inline, ContentJson, Scope, IsVisibleToClient, Version, CreatedAt, CreatedByUserId.  
- Enums: DeliverableType, DeliverableStatus, DeliverableFormat, DeliverableStorageType.  
- **IDeliverableService**: CreateDraft, GetByRun, GetById, Finalize; validación de permisos y Scope.  
- **IDeliverableComposer**: ComposeFromRun para un solo tipo (recomendado: AnalysisReport en Markdown guardado en ContentJson).  
- Trazabilidad mínima: en MVP se puede guardar en Deliverable un JSON con los IDs de DevFlowArtifact usados (snapshot) en lugar de tabla DeliverableArtifact.  
- UI: lista de deliverables en la página del proyecto y en la vista del run; botón “Generar entregable” (reporte); vista de contenido (Markdown).  
- Sin exportación a PDF/ZIP; solo ver y copiar.

**Fase 2 – Consolidación**  
- Tabla **DeliverableArtifact** y uso en Composer para registrar qué artifacts componen cada deliverable.  
- Soporte para más DeliverableType (BranchPlan, FunctionalSpec, TechnicalPackage).  
- Opcional: **DeliverableItem** si se necesita descomposición en secciones.  
- Mejoras de UI: filtros por tipo/estado, “Finalizar” desde la UI.

**Fase 3 – Exportaciones**  
- **IDeliverableExportService** y entidad **DeliverableExport** (opcional).  
- Export a PDF/HTML/ZIP según formato.  
- Botón “Descargar” que genere/recupere el archivo.

**Fase 4 – Código / repositorio (si aplica)**  
- Tipo CodeBootstrap; StorageType BlobReference o ExternalUrl para repo o ZIP de código.  
- Reglas claras: solo lectura/descarga para UserProject; ningún “apply” automático sobre la plataforma; InternalSystem y “apply” solo SuperUsuario con auditoría.

---

# 10. Recomendación final

**MVP recomendado para implementar primero**

- **Entidad única**: Deliverable (sin DeliverableArtifact en MVP; snapshot de artifact IDs en JSON en ContentJson o en un campo opcional).  
- **Un tipo inicial**: **AnalysisReport** (o “Report”) en **Markdown** guardado en **ContentJson** (StorageType = Inline).  
- **Flujo**: desde la vista del DevFlowRun, el usuario (con permiso) puede “Generar entregable” → el Composer toma los DevFlowArtifact del run, genera un Markdown consolidado (por ejemplo por etapa) y crea un Deliverable en Draft con ese contenido; luego el usuario puede “Finalizar”.  
- **Visibilidad**: según rol y Scope; UserProject y IsVisibleToClient para entregables finalizados.  
- **UI**: lista de entregables en proyecto y en run; vista “Ver” que renderice el Markdown del Report.  
- **Sin** export PDF/ZIP ni tabla DeliverableArtifact en esta fase; se añaden en Fase 2 y 3 sin romper el modelo.

Con esto el módulo queda integrado a la arquitectura actual (Project → DevFlowRun → DevFlowArtifact → Deliverable), mantiene la separación UserProject/InternalSystem y la regla de seguridad, y permite crecer después con más tipos, tabla puente y exportaciones.
