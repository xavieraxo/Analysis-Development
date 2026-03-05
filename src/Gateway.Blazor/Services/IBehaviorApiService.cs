using Gateway.Blazor.Models;

namespace Gateway.Blazor.Services;

public interface IBehaviorApiService
{
    Task<List<Behavior>> GetAllAsync();
    Task<Behavior?> GetByRoleAsync(AgentRole role);
    Task<Behavior?> UpsertAsync(Behavior behavior);
}

