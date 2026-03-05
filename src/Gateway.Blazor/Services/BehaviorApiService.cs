using Gateway.Blazor.Models;

namespace Gateway.Blazor.Services;

public class BehaviorApiService : IBehaviorApiService
{
    private readonly IApiService _api;

    public BehaviorApiService(IApiService api)
    {
        _api = api;
    }

    public async Task<List<Behavior>> GetAllAsync()
    {
        return await _api.GetAsync<List<Behavior>>("/api/behaviors") ?? new List<Behavior>();
    }

    public async Task<Behavior?> GetByRoleAsync(AgentRole role)
    {
        return await _api.GetAsync<Behavior>($"/api/behaviors/{role}");
    }

    public async Task<Behavior?> UpsertAsync(Behavior behavior)
    {
        return await _api.PostAsync<Behavior>("/api/behaviors", behavior);
    }
}

