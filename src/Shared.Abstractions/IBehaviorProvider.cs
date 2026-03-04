namespace Shared.Abstractions;

public interface IBehaviorProvider
{
    Task<BehaviorProfile> GetBehaviorAsync(AgentRole role, CancellationToken ct = default);
}

