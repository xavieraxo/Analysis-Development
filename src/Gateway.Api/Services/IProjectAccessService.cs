namespace Gateway.Api.Services;

public interface IProjectAccessService
{
    Task<bool> CanAccessProjectAsync(int currentUserId, int projectId, CancellationToken cancellationToken = default);
}

