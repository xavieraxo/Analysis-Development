using Gateway.Api.DTOs;
using Shared.Abstractions;

namespace Gateway.Api.Services;

public interface IBehaviorService : IBehaviorProvider
{
    Task<List<BehaviorDto>> GetAllAsync(CancellationToken ct = default);
    Task<BehaviorDto> GetByRoleAsync(AgentRole role, CancellationToken ct = default, bool allowFallback = true);
    Task<BehaviorDto> UpsertAsync(BehaviorUpsertRequest request, CancellationToken ct = default);
}

