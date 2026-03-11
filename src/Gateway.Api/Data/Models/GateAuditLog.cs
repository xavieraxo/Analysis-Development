namespace Data.Models;

public class GateAuditLog
{
    public int Id { get; set; }

    public int GateId { get; set; }
    public DevFlowGate Gate { get; set; } = null!;

    public int ActorUserId { get; set; }
    public ApplicationUser ActorUser { get; set; } = null!;

    public string Action { get; set; } = string.Empty;
    public bool IsForcedBySuperUser { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
}

