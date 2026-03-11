using Data.Models;

namespace Gateway.Api.Services;

public interface IAuditService
{
    Task LogGateActionAsync(
        int gateId,
        int actorUserId,
        string action,
        bool isForcedBySuperUser,
        string? comment,
        CancellationToken cancellationToken = default);

    Task LogInternalDevFlowActionAsync(
        int runId,
        int actorUserId,
        string action,
        string resourceType,
        int resourceId,
        bool success,
        string? comment,
        CancellationToken cancellationToken = default);
}

