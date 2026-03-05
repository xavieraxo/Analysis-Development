using Gateway.Blazor.Models;

namespace Gateway.Blazor.Services;

/// <summary>
/// Implementación del cliente API para DevFlow.
/// </summary>
public class DevFlowApiService : IDevFlowApiService
{
    private readonly IApiService _api;

    public DevFlowApiService(IApiService api)
    {
        _api = api;
    }

    public async Task<PagedResponse<DevFlowRunListItem>?> ListRunsAsync(DevFlowRunsQuery query, CancellationToken ct = default)
    {
        var queryString = BuildQueryString(query);
        var endpoint = $"/api/devflow/runs{queryString}";
        return await _api.GetAsync<PagedResponse<DevFlowRunListItem>>(endpoint);
    }

    public async Task<DevFlowRunResponse?> CreateRunAsync(CreateDevFlowRunRequest request, CancellationToken ct = default)
    {
        return await _api.PostAsync<DevFlowRunResponse>("/api/devflow/runs", request);
    }

    public async Task<DevFlowRunDetailResponse?> GetRunAsync(int id, CancellationToken ct = default)
    {
        return await _api.GetAsync<DevFlowRunDetailResponse>($"/api/devflow/runs/{id}");
    }

    public async Task<ExecuteStageResponse?> ExecuteStageAsync(int id, ExecuteStageRequest request, CancellationToken ct = default)
    {
        return await _api.PostAsync<ExecuteStageResponse>($"/api/devflow/runs/{id}/execute-stage", request);
    }

    public async Task<ApproveGateResponse?> ApproveGateAsync(int id, ApproveGateRequest request, CancellationToken ct = default)
    {
        return await _api.PostAsync<ApproveGateResponse>($"/api/devflow/runs/{id}/approve", request);
    }

    private static string BuildQueryString(DevFlowRunsQuery query)
    {
        var parameters = new List<string>
        {
            $"page={query.Page}",
            $"pageSize={query.PageSize}"
        };
        if (query.ProjectId.HasValue)
            parameters.Add($"projectId={query.ProjectId.Value}");
        if (query.Status.HasValue)
            parameters.Add($"status={(int)query.Status.Value}");
        if (query.Stage.HasValue)
            parameters.Add($"stage={(int)query.Stage.Value}");
        return "?" + string.Join("&", parameters);
    }
}
