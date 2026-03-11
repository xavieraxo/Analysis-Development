namespace Data.Models;

public class InternalDevFlowAuditLog
{
    public int Id { get; set; }

    public int ActorUserId { get; set; }
    public ApplicationUser ActorUser { get; set; } = null!;

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;
    public int ResourceId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }

    public string? Comment { get; set; }
}

