using Data;
using Data.Models;

namespace Gateway.Api.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogGateActionAsync(
        int gateId,
        int actorUserId,
        string action,
        bool isForcedBySuperUser,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var entry = new GateAuditLog
        {
            GateId = gateId,
            ActorUserId = actorUserId,
            Action = action,
            IsForcedBySuperUser = isForcedBySuperUser,
            Timestamp = DateTime.UtcNow,
            Comment = comment
        };

        _db.GateAuditLogs.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task LogInternalDevFlowActionAsync(
        int runId,
        int actorUserId,
        string action,
        string resourceType,
        int resourceId,
        bool success,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var entry = new InternalDevFlowAuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Timestamp = DateTime.UtcNow,
            Success = success,
            Comment = comment
        };

        _db.InternalDevFlowAuditLogs.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

