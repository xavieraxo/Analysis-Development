using Gateway.Api.DTOs;

namespace Gateway.Api.Services;

/// <summary>
/// Servicio para operaciones de DevFlow.
/// </summary>
public interface IDevFlowService
{
    /// <summary>
    /// Crea un DevFlow Run con estado inicial Created.
    /// </summary>
    /// <param name="request">Datos del run.</param>
    /// <param name="createdByUserId">ID del usuario Identity que crea el run.</param>
    /// <returns>El run creado o null si ProjectId no existe.</returns>
    Task<DevFlowRunResponse?> CreateRunAsync(CreateDevFlowRunRequest request, int createdByUserId);

    /// <summary>
    /// Obtiene un DevFlow Run por Id con sus artifacts y gates.
    /// </summary>
    /// <param name="id">Id del run.</param>
    /// <returns>El run detallado o null si no existe.</returns>
    Task<DevFlowRunDetailResponse?> GetRunByIdAsync(int id);

    /// <summary>
    /// Lista DevFlow Runs con filtros y paginación.
    /// </summary>
    /// <param name="query">Parámetros de filtro y paginación.</param>
    /// <returns>Respuesta paginada. Null si pageSize fuera de rango.</returns>
    Task<PagedResponse<DevFlowRunListItem>?> GetRunsAsync(DevFlowRunsQueryParams query);

    /// <summary>
    /// Ejecuta una etapa del DevFlow: invoca al agente, persiste artifact y avanza el run.
    /// </summary>
    /// <param name="runId">ID del run.</param>
    /// <param name="request">Input y etapa opcional.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado con Response (200) o ErrorMessage y HttpStatusCode (404, 409, 400).</returns>
    Task<ExecuteStageResult> ExecuteStageAsync(int runId, ExecuteStageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aprobar o rechazar un gate del DevFlow. Crea o actualiza el gate.
    /// </summary>
    /// <param name="runId">ID del run.</param>
    /// <param name="request">Stage, approved y comment opcional.</param>
    /// <param name="decidedByUserId">ID del usuario que toma la decisión.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado con Response (200) o ErrorMessage y HttpStatusCode (404, 400).</returns>
    Task<ApproveGateResult> ApproveGateAsync(int runId, ApproveGateRequest request, int decidedByUserId, CancellationToken cancellationToken = default);
}
