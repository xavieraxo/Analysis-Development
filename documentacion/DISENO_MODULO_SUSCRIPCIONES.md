# Diseño técnico: Módulo de suscripciones y visibilidad por plan

Documento de arquitectura para integrar planes y suscripciones en la arquitectura actual sin reemplazar roles ni ownership. Sin implementación; solo diseño.

---

# 1. Resumen ejecutivo

**Qué es el módulo de suscripciones**  
Es la capa que define **qué puede consumir** cada usuario o empresa según su **plan**: visibilidad de resultados, descargas, tipos de entregables, y límites de uso (proyectos activos, usuarios por empresa). El plan no sustituye al rol: el **rol** define ámbito y capacidad administrativa; la **suscripción** define techo de consumo, visibilidad y descarga.

**Por qué hace falta**  
- Diferenciar oferta Free de planes de pago sin tocar la lógica de roles ni de DevFlowScope.  
- Limitar proyectos activos por usuario individual o por empresa.  
- Limitar usuarios por empresa según plan.  
- Controlar qué puede **ver** y **descargar** cada usuario (informes básicos, reportes, todos los deliverables).  
- Permitir evolución: nombres de planes editables, códigos internos estables, futura integración de pagos.  
- Mantener una única fuente de verdad en backend para límites y permisos de consumo (no depender solo de la UI).

---

# 2. Arquitectura conceptual

**Encaje con lo existente**

- **Usuario (ApplicationUser)**  
  - Puede tener **suscripción individual** (Subscription con OwnerType = Individual, OwnerUserId no nulo).  
  - Si pertenece a una **Empresa**, el **consumo** en proyectos de esa empresa se rige por la **suscripción de la Empresa**, no por la suya individual (salvo que use proyectos propios individuales).

- **Empresa**  
  - Puede tener **una suscripción** (Subscription con OwnerType = Empresa, EmpresaId no nulo).  
  - Esa suscripción define: máximo de usuarios asociados, máximo de proyectos activos, y qué puede ver/descargar todo usuario que opere en proyectos de esa empresa.

- **Project**  
  - Sigue con ownership Individual o Empresa.  
  - **Crear** un proyecto nuevo debe validarse contra el plan: usuario individual (límite de proyectos activos según su suscripción o plan Free); empresa (límite según suscripción de la Empresa).

- **DevFlow / Deliverable**  
  - No cambia la estructura; sí la **visibilidad y descarga**: según el plan efectivo del usuario (individual o de la empresa del proyecto), se permite o no ver ciertos tipos de entregables y descargar.

**Flujo de “plan efectivo”**  
- Si el usuario actúa sobre un **proyecto propio (ownership Individual)** → plan efectivo = su **suscripción individual** (o Free si no tiene suscripción).  
- Si el usuario actúa sobre un **proyecto de una Empresa** → plan efectivo = **suscripción de esa Empresa**.  
- SuperUsuario puede ver todo por rol; aun así el **modelo de planes** sigue aplicando para el resto del sistema (límites, auditoría, reportes); no se usa el plan para restringir a SuperUsuario en backend si se decide política de exención, pero la regla debe quedar explícita.

---

# 3. Entidades recomendadas

## 3.1 SubscriptionPlan (catálogo de planes)

**Propósito**  
Definir los planes disponibles en el sistema: código interno estable, nombre visible editable, tipo (individual vs empresa), límites y capacidades (ver informes, descargar, deliverables completos, max usuarios, max proyectos).

**Campos recomendados**

| Campo                | Tipo                 | Descripción |
|----------------------|----------------------|-------------|
| Id                   | int                  | PK. |
| PlanCode             | string               | Código interno estable (ej. Free, IndividualBasic, IndividualPremium, EmpresaBasic, EmpresaMid, EmpresaFull). Único. |
| DisplayName          | string               | Nombre visible (editable): "Plan Free", "Plan 1", "Empresa Básica", etc. |
| OwnerType            | enum                 | Individual \| Empresa. |
| MaxActiveProjects    | int?                 | Null = ilimitado. Para individual: 1 en Free; para empresa según plan. |
| MaxUsers             | int?                 | Solo para planes Empresa. Null = ilimitado. |
| CanViewBasicReports  | bool                 | Ver resultados/informes básicos. |
| CanDownloadReports   | bool                 | Descargar informes/reportes. |
| CanAccessAllDeliverables | bool             | Acceso a todos los tipos de entregables (no solo informes/reportes). |
| CanDownloadDeliverables | bool              | Descargar entregables (export). |
| IsActive             | bool                 | Si el plan está disponible para asignación. |
| SortOrder            | int                  | Orden en listados. |
| CreatedAt            | DateTime             | UTC. |
| UpdatedAt            | DateTime?            | UTC. |

**Relaciones**  
- 1:N con **Subscription** (cada suscripción apunta a un plan).

**MVP**  
Sí; es el catálogo mínimo. Se pobla con los 6 planes (Free, Plan1–2 individuales, Plan3–5 empresa).

---

## 3.2 Subscription (suscripción activa de un usuario o empresa)

**Propósito**  
Vincular un **usuario individual** o una **Empresa** con un **SubscriptionPlan** y un estado (activa, cancelada, vencida). Una suscripción es “quién tiene qué plan”.

**Campos recomendados**

| Campo           | Tipo              | Descripción |
|-----------------|-------------------|-------------|
| Id              | int               | PK. |
| SubscriptionPlanId | int            | FK a SubscriptionPlan. |
| OwnerType       | enum              | Individual \| Empresa. |
| OwnerUserId     | int?              | Si OwnerType = Individual, FK a ApplicationUser. |
| EmpresaId       | int?              | Si OwnerType = Empresa, FK a Empresa. |
| Status          | enum              | Active, Cancelled, Expired, Trial. |
| StartedAt       | DateTime          | UTC. |
| ExpiresAt       | DateTime?         | Null = sin vencimiento por ahora. |
| CreatedAt       | DateTime          | UTC. |
| UpdatedAt       | DateTime?         | UTC. |

**Relaciones**  
- N:1 con SubscriptionPlan.  
- N:1 con ApplicationUser (OwnerUserId) cuando OwnerType = Individual.  
- N:1 con Empresa (EmpresaId) cuando OwnerType = Empresa.  
- Unique: (OwnerType, OwnerUserId) cuando Individual; (OwnerType, EmpresaId) cuando Empresa (una suscripción activa por usuario o por empresa).

**MVP**  
Sí; sin suscripción no hay plan aplicable. Por defecto: usuario sin Subscription = plan Free; empresa sin Subscription = se puede tratar como no permitido o plan mínimo según regla de negocio.

---

## 3.3 SubscriptionFeature (opcional, fase posterior)

**Propósito**  
Modelar capacidades por plan de forma flexible (clave-valor o entidad por feature) para no hardcodear columnas booleanas. Ej.: "download_reports" = true, "max_projects" = 5.

**MVP**  
No; en MVP las capacidades van en SubscriptionPlan (columnas booleanas y Max*). Fase posterior si se necesita alta flexibilidad sin migrar esquema.

---

## 3.4 Consumo / límites (sin entidad obligatoria en MVP)

**Propósito**  
Saber “cuántos proyectos activos tiene este usuario/empresa” y “cuántos usuarios tiene esta empresa”. Eso se puede **calcular** desde datos existentes:  
- Proyectos activos: contar Projects con ownership del usuario o de la empresa y Status considerado “activo”.  
- Usuarios en empresa: contar EmpresaUsers por EmpresaId.  
No hace falta tabla de “consumo” en MVP; solo consultas. Si más adelante se requiere historial o cuotas por período, se puede añadir una entidad **UsageSnapshot** o **SubscriptionUsage** en fase posterior.

---

# 4. Enums recomendados

| Enum                    | Valores                    | Uso |
|-------------------------|----------------------------|-----|
| **SubscriptionPlanOwnerType** | Individual, Empresa   | Si el plan aplica a usuario individual o a empresa. |
| **SubscriptionOwnerType**     | Individual, Empresa   | A quién pertenece la suscripción (reutilizable si Plan y Subscription comparten significado). |
| **SubscriptionStatus**       | Active, Cancelled, Expired, Trial | Estado de la suscripción. |

- **Individual**: planes Free, Plan1, Plan2.  
- **Empresa**: planes Plan3, Plan4, Plan5.  
- **Active**: vigente. **Cancelled**: dada de baja. **Expired**: venció. **Trial**: período de prueba (opcional).

---

# 5. Servicios recomendados

## 5.1 ISubscriptionPlanService (catálogo)

**Responsabilidad**  
Mantener el catálogo de planes: listar, obtener por código, actualizar DisplayName y capacidades (Admin). No asigna suscripciones.

**Métodos principales**  
- `GetAllPlansAsync(ownerType?)` – lista planes activos, opcionalmente filtrado por Individual/Empresa.  
- `GetByCodeAsync(planCode)` – para resolver “Free”, “EmpresaFull”, etc.  
- `GetByIdAsync(planId)`.  
- `UpdateDisplayNameAsync(planId, displayName)` – solo Admin.

**Qué hace**  
- CRUD de planes; no decide qué plan tiene un usuario/empresa.

**Qué no hace**  
- No crea Subscription; no calcula límites ni visibilidad.

---

## 5.2 ISubscriptionService (suscripciones)

**Responsabilidad**  
Asignar y consultar suscripciones de usuarios y empresas; alta/baja de suscripción.

**Métodos principales**  
- `GetByUserAsync(userId)` – suscripción individual del usuario (null = Free implícito).  
- `GetByEmpresaAsync(empresaId)` – suscripción de la empresa.  
- `GetEffectivePlanForUserAsync(userId, projectId?)` – plan efectivo: si el usuario actúa en un proyecto de empresa, devuelve plan de la empresa; si proyecto propio, plan del usuario (o Free).  
- `AssignPlanToUserAsync(userId, planCode, status)` – asignar plan a usuario (Admin).  
- `AssignPlanToEmpresaAsync(empresaId, planCode, status)` – asignar plan a empresa (Admin).  
- `CancelAsync(subscriptionId)` – poner en Cancelled.

**Qué hace**  
- Resuelve “qué plan tiene este usuario/empresa” y “qué plan aplica en este contexto (proyecto)”.

**Qué no hace**  
- No valida límites al crear proyecto ni al descargar; eso es IPlanLimitService / ISubscriptionAccessService.

---

## 5.3 IPlanLimitService (límites de uso)

**Responsabilidad**  
Comprobar si se puede realizar una acción según el plan efectivo: creación de proyectos, agregar usuarios a empresa.

**Métodos principales**  
- `CanCreateProjectAsync(userId, ownerType, ownerId?)` – false si ya alcanzó el máximo de proyectos activos para ese plan (individual o empresa).  
- `GetRemainingProjectSlotsAsync(userId, ownerType, ownerId?)` – cuántos proyectos activos puede crear aún.  
- `CanAddUserToEmpresaAsync(empresaId)` – false si la empresa ya tiene MaxUsers según su plan.  
- `GetRemainingUserSlotsAsync(empresaId)` – cuántos usuarios puede agregar la empresa.

**Qué hace**  
- Usa Subscription + SubscriptionPlan + conteos (Projects activos, EmpresaUsers) para decidir sí/no y slots restantes.

**Qué no hace**  
- No decide si puede ver o descargar un deliverable; eso es ISubscriptionAccessService.

---

## 5.4 ISubscriptionAccessService (visibilidad y descarga)

**Responsabilidad**  
Decidir, según el plan efectivo del usuario en el contexto dado, si puede **ver** o **descargar** un tipo de contenido (informe básico, reporte, deliverable completo, export).

**Métodos principales**  
- `CanViewDeliverableAsync(userId, projectId, deliverableId)` – si el plan permite ver ese deliverable (tipo + IsVisibleToClient).  
- `CanDownloadDeliverableAsync(userId, projectId, deliverableId)` – si el plan permite descargar.  
- `CanViewBasicReportsAsync(userId, projectId)` – si puede ver resultados/informes básicos.  
- `CanDownloadReportsAsync(userId, projectId)` – si puede descargar reportes.  
- `GetVisibleDeliverableTypesAsync(userId, projectId)` – lista de tipos de deliverable que el usuario puede ver en ese proyecto según plan.

**Qué hace**  
- Centraliza la lógica “plan → capacidades de visibilidad y descarga” para que backend y UI la usen igual.

**Qué no hace**  
- No asigna suscripciones ni gestiona límites de proyectos/usuarios.

---

# 6. Reglas de negocio

1. **El plan no reemplaza al rol**  
   Rol = ámbito y capacidad administrativa (SuperUsuario, AppAdmin, TenantAdmin, OperadorEmpresa, User). Suscripción = techo de consumo, visibilidad y descarga.

2. **Un usuario individual puede tener una suscripción propia**  
   Subscription con OwnerType = Individual, OwnerUserId = userId. Si no tiene suscripción, se considera plan Free.

3. **Una Empresa puede tener una suscripción propia**  
   Subscription con OwnerType = Empresa, EmpresaId = empresaId. Si la empresa no tiene suscripción, se puede definir política (bloquear uso o tratar como plan mínimo).

4. **Usuarios de la Empresa heredan capacidades según la suscripción de la Empresa**  
   Al operar sobre proyectos de la empresa, el plan efectivo es el de la empresa; el rol del usuario solo define qué acciones administrativas puede hacer dentro de ese techo.

5. **Free: solo ver resultados/informes básicos; no descargar**  
   CanViewBasicReports = true; CanDownloadReports = false; no acceso a todos los deliverables.

6. **Plan 1 (individual básico): ver todos los informes/reportes y descargarlos**  
   CanViewBasicReports, CanDownloadReports = true; CanAccessAllDeliverables = false.

7. **Plan 2 (individual premium): todos los entregables y descarga**  
   CanAccessAllDeliverables, CanDownloadDeliverables = true.

8. **Planes empresa (3, 4, 5): entrega completa (informes, reportes, diseños, entregables)**  
   Capacidades de visualización y descarga completas; se diferencian por MaxUsers y MaxActiveProjects.

9. **Plan 3: hasta 2 usuarios, hasta 5 proyectos activos.**  
10. **Plan 4: hasta 10 usuarios, hasta 10 proyectos activos.**  
11. **Plan 5: usuarios y proyectos ilimitados (MaxUsers/MaxActiveProjects = null).**  
12. **Deliverables visibles y descargables dependen del plan en backend**  
    No solo de la UI; toda comprobación de “¿puede ver/descargar?” debe pasar por ISubscriptionAccessService (o equivalente) en API.

13. **SuperUsuario**  
    Puede ver todo por rol; el modelo de planes sigue vigente para el resto; se puede definir si SuperUsuario está exento de comprobaciones de plan en backend (ej. siempre permitir) y documentarlo.

---

# 7. Visibilidad por plan

| Plan   | Ver informes básicos | Descargar informes/reportes | Ver todos los reportes | Acceso a todos los deliverables | Descargar deliverables | Max proyectos (individual) | Max usuarios (empresa) | Max proyectos (empresa) |
|--------|----------------------|-----------------------------|-------------------------|----------------------------------|------------------------|----------------------------|------------------------|--------------------------|
| Free   | Sí                   | No                          | No                      | No                               | No                    | 1                          | —                      | —                        |
| Plan 1 | Sí                   | Sí                          | Sí                      | No                               | No (solo reportes)     | 1                          | —                      | —                        |
| Plan 2 | Sí                   | Sí                          | Sí                      | Sí                               | Sí                     | 1 (o más si se define)      | —                      | —                        |
| Plan 3 | Sí                   | Sí                          | Sí                      | Sí                               | Sí                     | —                          | 2                      | 5                        |
| Plan 4 | Sí                   | Sí                          | Sí                      | Sí                               | Sí                     | —                          | 10                     | 10                       |
| Plan 5 | Sí                   | Sí                          | Sí                      | Sí                               | Sí                     | —                          | Ilimitado               | Ilimitado                |

- **Ver informes básicos**: resultados mínimos en pantalla (resumen, análisis básico).  
- **Descargar informes/reportes**: export de reportes/informes (no necesariamente todos los tipos de deliverable).  
- **Acceso a todos los deliverables**: ver (y según columna siguiente descargar) cualquier tipo (BranchPlan, TechnicalPackage, etc.).  
- Límites de proyectos “activos”: definir qué ProjectStatus cuentan (ej. InProgress, OnHold).  
- Planes empresa: Max usuarios = miembros en EmpresaUsers; Max proyectos = proyectos con ownership Empresa y en estado activo.

---

# 8. Relación entre plan y rol

- **Rol**  
  - Define **quién puede hacer qué a nivel de sistema**: SuperUsuario (config interno), AppAdmin (gestión global), TenantAdmin (gestión de su empresa), OperadorEmpresa (operar en proyectos de la empresa), User (sus proyectos).  
  - No define cuántos proyectos puede tener ni si puede descargar; eso es el **plan**.

- **Ownership**  
  - Project es de un usuario (Individual) o de una Empresa.  
  - No cambia con el módulo de suscripciones; el plan solo **limita** cuántos proyectos activos y **qué** puede ver/descargar.

- **Scope (DevFlowScope)**  
  - UserProject vs InternalSystem; solo SuperUsuario para InternalSystem.  
  - Independiente del plan; el plan no otorga acceso a InternalSystem.

- **Suscripción**  
  - Define **límites** (proyectos, usuarios) y **capacidades de consumo** (ver/descargar por tipo de contenido).  
  - AppAdmin puede administrar planes y suscripciones, pero si AppAdmin actúa como usuario normal en un proyecto, su visibilidad/descarga puede depender del plan efectivo (o se exime por rol; debe quedar definido).  
  - TenantAdmin opera dentro de su Empresa; el techo de esa empresa lo da la **suscripción de la Empresa**, no un plan “TenantAdmin”.  
  - OperadorEmpresa: mismo techo que la empresa; el rol solo define qué acciones puede ejecutar dentro de ese techo.  
  - SuperUsuario: puede ver todo; el modelo de planes no se usa para restringirlo; el resto del sistema sí se rige por planes.

---

# 9. Diseño de UI

- **Plan actual del usuario o empresa**  
  - En perfil o en “Mi cuenta”: “Tu plan: Plan Free” / “Plan 1” / “Empresa Básica”, etc. (DisplayName del plan).  
  - Para TenantAdmin/AppAdmin: en la ficha de la empresa, “Plan: Empresa Básica” y estado de la suscripción.

- **Límites consumidos**  
  - Usuario individual: “Proyectos activos: 1/1” (Free/Plan1) o “1/1” (Plan2 si se limita a 1).  
  - Empresa: “Usuarios: 2/2”, “Proyectos activos: 3/5” (ej. Plan 3).  
  - Barras o textos claros; mensaje “Has alcanzado el límite de proyectos” o “Límite de usuarios alcanzado” al intentar crear o invitar.

- **Botones y mensajes de upgrade**  
  - Si no puede descargar: botón “Descargar” deshabilitado con tooltip “Disponible en Plan 1 o superior”.  
  - Si no puede ver un deliverable: no mostrarlo en la lista o mostrarlo con candado y “Upgrade para acceder”.  
  - En lista de entregables: filtrar o marcar los que no puede ver/descargar según plan.

- **Diferenciación Free vs individual premium vs empresa**  
  - Free: mensaje tipo “Mejora tu experiencia con Plan 1: descarga informes y reportes.”  
  - Individual premium: “Acceso completo a todos los entregables.”  
  - Empresa: “Plan Empresa: X usuarios, Y proyectos activos”; si está al límite, “Contacta para ampliar tu plan”.

- **Admin**  
  - Pantalla de planes: listado con PlanCode, DisplayName, límites, capacidades; edición de DisplayName.  
  - Asignar plan a usuario (por userId) o a empresa (por empresaId); estado Active/Cancelled/Expired.  
  - Vista de uso: por empresa, “X/Y usuarios”, “Z/W proyectos activos”.

---

# 10. Estrategia de implementación por fases

**Fase 1 – Catálogo y suscripción mínima**  
- Entidades SubscriptionPlan y Subscription; enums.  
- Poblado inicial de planes (Free, Plan1, Plan2, Plan3, Plan4, Plan5) con PlanCode estable y DisplayName.  
- ISubscriptionPlanService, ISubscriptionService.  
- Asignación manual de plan a usuario y a empresa (Admin).  
- Resolución de “plan efectivo” por usuario y por contexto de proyecto.  
- Sin validación de límites aún en creación de proyectos; solo modelo de datos y consulta de plan.

**Fase 2 – Límites de proyectos y usuarios**  
- IPlanLimitService: CanCreateProjectAsync, CanAddUserToEmpresaAsync, GetRemaining*SlotsAsync.  
- Validar en backend al crear Project (individual y empresa) y al agregar EmpresaUser.  
- UI: mostrar límites consumidos y deshabilitar “Crear proyecto” / “Invitar usuario” cuando corresponda.

**Fase 3 – Visibilidad y descarga de deliverables**  
- ISubscriptionAccessService: CanViewDeliverableAsync, CanDownloadDeliverableAsync, CanViewBasicReportsAsync, CanDownloadReportsAsync.  
- Endpoints de deliverables y de descarga que consulten este servicio antes de devolver contenido o archivo.  
- UI: ocultar o deshabilitar según plan; mensajes de upgrade.

**Fase 4 – Upgrade/downgrade y administración completa**  
- Flujos de cambio de plan (Admin); opcionalmente flujo de “solicitar upgrade” para el usuario.  
- Histórico o auditoría de cambios de suscripción si se requiere.  
- Integración con pasarela de pagos (fuera de este diseño; solo preparar PlanCode y estados).

---

# 11. Recomendación final (MVP)

**MVP recomendado para implementar primero**

1. **Catálogo de planes**  
   - Entidad SubscriptionPlan con PlanCode (estable), DisplayName, OwnerType, MaxActiveProjects, MaxUsers, CanViewBasicReports, CanDownloadReports, CanAccessAllDeliverables, CanDownloadDeliverables.  
   - Poblado con los 6 planes (Free, Plan1, Plan2, Plan3, Plan4, Plan5).

2. **Suscripción individual y por empresa**  
   - Entidad Subscription (SubscriptionPlanId, OwnerType, OwnerUserId/EmpresaId, Status).  
   - Regla: usuario sin suscripción = Free; empresa sin suscripción = no permitir uso de proyectos de empresa hasta asignar plan (o plan mínimo definido).  
   - ISubscriptionService: GetByUserAsync, GetByEmpresaAsync, GetEffectivePlanForUserAsync(userId, projectId), AssignPlanToUserAsync, AssignPlanToEmpresaAsync (Admin).

3. **Control de visibilidad y descarga sobre Deliverables**  
   - ISubscriptionAccessService con CanViewDeliverableAsync, CanDownloadDeliverableAsync (y si aplica CanViewBasicReportsAsync, CanDownloadReportsAsync).  
   - Endpoints que devuelven o permiten descargar deliverables deben validar con este servicio antes de devolver contenido.

4. **Límite de proyectos activos**  
   - IPlanLimitService: CanCreateProjectAsync, GetRemainingProjectSlotsAsync.  
   - Al crear proyecto (individual o empresa), validar en backend y rechazar si se supera el límite del plan.

5. **Sin pagos**  
   - Asignación de planes solo por Admin (manual).  
   - No integración con pasarela; ExpiresAt opcional para futuras pruebas de vencimiento.

Con esto el módulo queda integrado con usuarios, empresas, proyectos y deliverables; roles y ownership se mantienen; la validación de límites y de visibilidad/descarga queda en backend como fuente de verdad; y se puede extender en fases posteriores con más límites (usuarios por empresa), upgrade/downgrade y pagos.
