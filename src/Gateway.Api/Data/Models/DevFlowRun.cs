namespace Data.Models;

/// <summary>
/// Etapas del pipeline DevFlow (UR → PM → PO → DEV).
/// </summary>
public enum DevFlowStage
{
    UR = 0,   // Usuario Representante
    PM = 1,   // Project Manager
    PO = 2,   // Product Owner
    DEV = 3   // Desarrollador
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
/// Estado de aprobación de un gate entre etapas del DevFlow.
/// </summary>
public enum DevFlowGateStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// Representa una ejecución del flujo DevFlow: idea de cambio, estado y etapas.
/// </summary>
public class DevFlowRun
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DevFlowRunStatus Status { get; set; } = DevFlowRunStatus.Created;
    public DevFlowStage? CurrentStage { get; set; }
    public int CreatedByUserId { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DevFlowScope Scope { get; set; } = DevFlowScope.UserProject;
    public bool IsMigrated { get; set; }

    public List<DevFlowArtifact> Artifacts { get; set; } = new();
    public List<DevFlowGate> Gates { get; set; } = new();
    public BranchPlan? BranchPlan { get; set; }
}

public enum DevFlowScope
{
    UserProject = 0,
    InternalSystem = 1
}

