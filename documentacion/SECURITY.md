## Seguridad de DevFlow y auto-modificación

### Regla definitiva

- **Solo el SuperUsuario** puede usar DevFlow para producir cambios **aplicables sobre la propia plataforma** (auto-modificación).
- Ningún otro rol (Usuario, TenantAdmin, OperadorEmpresa, AppAdmin) puede:
  - Modificar archivos de la app.
  - Escribir fuera del almacenamiento funcional permitido.
  - Modificar prompts, behaviors, pipeline o configuraciones internas.
  - Generar cambios aplicables sobre la app.
  - Acceder a la base de datos fuera de los servicios normales de negocio.

### Directiva técnica obligatoria

Ningún artifact, output de agente, branch plan, plan técnico, prompt, resumen o resultado del pipeline podrá ejecutarse, aplicarse o transformarse automáticamente en un cambio del sistema salvo que **todas** estas condiciones se cumplan:

1. El flujo sea de tipo interno/autodesarrollo (InternalSystem).
2. El actor sea **SuperUsuario**.
3. Exista autorización explícita para aplicar ese artifact.
4. Quede auditoría completa (quién, cuándo, qué se aplicó, resultado).
5. El mecanismo de aplicación esté aislado del flujo normal de usuario.

### Separación de DevFlow

- `DevFlowScope.UserProject`:
  - Runs asociados a proyectos de usuario.
  - Artifacts y branch plans son **solo datos de negocio**, nunca se aplican directamente al sistema.
  - Acceso según ownership/asignación del proyecto.
- `DevFlowScope.InternalSystem`:
  - Runs de autodesarrollo interno.
  - **Solo SuperUsuario** puede crearlos, ejecutarlos, aprobar gates o aplicar artifacts.
  - Todas las acciones relevantes deben pasar por `IAuditService` (`GateAuditLog` y `InternalDevFlowAuditLog`).

### Opción B endurecida para `/api/chat/run`

- Requiere `projectId` obligatorio.
- Si el usuario **no** es SuperUsuario:
  - El chat se ejecuta en modo **exploratorio**.
  - No se persiste `Summary` ni `InternalConversationsJson` en el `Project`.
  - La UI de `Project.razor` lo indica explícitamente al usuario.
- Si el usuario **es** SuperUsuario:
  - Se mantiene el comportamiento actual: se guarda el análisis en el proyecto.

---

## Checklist de PR (seguridad y auto-modificación)

Antes de aprobar un PR, verifica al menos lo siguiente:

1. **Outputs de agentes / DevFlow**
   - [ ] ¿Algún artifact, branch plan o resultado de DevFlow se usa para modificar código, configuraciones, behaviors o pipeline?
   - [ ] Si la respuesta es sí, ¿está restringido a `DevFlowScope.InternalSystem` y a SuperUsuario?
   - [ ] ¿Pasa por `IAuditService` con registros en `GateAuditLog` / `InternalDevFlowAuditLog`?

2. **Endpoints y servicios sensibles**
   - [ ] ¿Algún endpoint nuevo o modificado permite cambiar configuraciones internas (`/api/configurations`, `/api/behaviors`, `/api/admin/*`)?
   - [ ] ¿Tienen `Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)` cuando corresponde?
   - [ ] ¿Los endpoints de administración funcional (`/api/admin/projects`, `/api/admin/users`) usan `AppAdminOrSuperUserPolicy` y no exponen mecanismos de auto-modificación?

3. **Chat y logging**
   - [ ] `/api/chat/run` exige `projectId` y valida que el proyecto pertenece al usuario.
   - [ ] Para usuarios no SuperUsuario, el chat está en modo exploratorio (sin persistencia en `Project`).
   - [ ] Cualquier escritura en disco por `AgentLoggingService` se considera **logs operativos**, no aplicación de cambios.

4. **DevFlowScope y acceso**
   - [ ] Los DTOs de DevFlow (`DevFlowRunResponse`, `DevFlowRunDetailResponse`) exponen `Scope` y el UI lo respeta donde sea relevante.
   - [ ] No hay código que trate un run `InternalSystem` como si fuera de usuario normal.

5. **Roles y policies**
   - [ ] Se usan los roles correctos (`SuperUsuario`, `AppAdmin`, `TenantAdmin`, `OperadorEmpresa`) de acuerdo a su responsabilidad.
   - [ ] Se usan las policies apropiadas (`SuperUserOnlyPolicy`, `AppAdminOrSuperUserPolicy`, etc.) en cada endpoint.

