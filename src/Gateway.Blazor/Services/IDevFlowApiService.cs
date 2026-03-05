using Gateway.Blazor.Models;

namespace Gateway.Blazor.Services;

/// <summary>
/// Cliente API para DevFlow.
/// </summary>
public interface IDevFlowApiService
{
    /// <summary>
    /// Lista DevFlow Runs con filtros opcionales.
    /// </summary>
    Task<PagedResponse<DevFlowRunListItem>?> ListRunsAsync(DevFlowRunsQuery query, CancellationToken ct = default);

    /// <summary>
    /// Crea un nuevo DevFlow Run.
    /// </summary>
    Task<DevFlowRunResponse?> CreateRunAsync(CreateDevFlowRunRequest request, CancellationToken ct = default);

    /// <summary>
    /// Obtiene el detalle de un DevFlow Run.
    /// </summary>
    Task<DevFlowRunDetailResponse?> GetRunAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Ejecuta la etapa actual del run.
    /// </summary>
    Task<ExecuteStageResponse?> ExecuteStageAsync(int id, ExecuteStageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Aprueba o rechaza un gate.
    /// </summary>
    Task<ApproveGateResponse?> ApproveGateAsync(int id, ApproveGateRequest request, CancellationToken ct = default);
}
