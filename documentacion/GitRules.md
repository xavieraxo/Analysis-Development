# Q-ADRA ERP - PROTOCOLO DE GIT

## 1. Estructura de Ramas
- **Rama Principal:** `master` (Solo lectura para la IA).
- **Ramas de Tarea:** Deben seguir el patrón `feature/#<ID_SOLICITUD>`.
- **Ramas Debug & Fix:** Patrón `fixing-debug/#NN` (NN correlativo). Registrar el número activo en [`Q-adra_bitacora_task_list.md`](Q-adra_bitacora_task_list.md).
- **Numeración:** Comienza en 1 e incrementa de forma secuencial según el análisis del PM.

## 2. Flujo de Trabajo de Antigravity (IA)
Para cada tarea asignada, la IA debe seguir este orden lógico sin desviaciones:
1. **Sincronización:** `git checkout master` -> `git pull`.
2. **Creación de Rama:** `git checkout -b feature/#<ID>`.
3. **Desarrollo:** Realizar los cambios solicitados en el código o documentación.
4. **Finalización de Tarea:** La IA **NO** debe ejecutar `git add .` ni `git commit`. 
5. **Entrega:** Sugerir un mensaje de commit descriptivo y esperar la revisión humana.

## 3. Reglas de Oro
- **Prohibido:** Ejecutar comandos de confirmación (commit) o subida (push).
- **Aislamiento:** Cada rama corresponde a un único Hito o Historia de Usuario.
- **Limpieza:** Antes de cada nueva tarea, siempre volver a `master` y actualizar.