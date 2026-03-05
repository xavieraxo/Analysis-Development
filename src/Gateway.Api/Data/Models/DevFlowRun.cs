namespace Data.Models;

/// <summary>
/// Etapas del pipeline DevFlow (UR → PM → PO → DEV).
/// </summary>
public enum DevFlowStage
{
    UR = 0,
    PM = 1,
    PO = 2,
    DEV = 3
}

/// <summary>
/// Estado de un DevFlowRun.
/// </summary>
public enum DevFlowRunStatus
{
    Created = 0,
    InProgress = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>
/// Representa una ejecución del flujo DevFlow.
/// </summary>
public class DevFlowRun
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DevFlowRunStatus Status { get; set; } = DevFlowRunStatus.Created;
    public DevFlowStage? CurrentStage { get; set; }
    public int CreatedByUserId { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<DevFlowArtifact> Artifacts { get; set; } = new();
}
