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
