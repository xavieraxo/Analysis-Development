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
}
