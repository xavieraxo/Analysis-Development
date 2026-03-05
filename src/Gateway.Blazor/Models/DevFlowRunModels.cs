namespace Gateway.Blazor.Models;

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
/// Estado de un DevFlow Run.
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
/// Item de lista para DevFlow Runs.
/// </summary>
public class DevFlowRunListItem
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DevFlowRunStatus Status { get; set; }
    public DevFlowStage? CurrentStage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ArtifactsCount { get; set; }
    public int GatesCount { get; set; }
}

/// <summary>
/// Respuesta paginada genérica.
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}

/// <summary>
/// Parámetros de consulta para listar DevFlow Runs.
/// </summary>
public class DevFlowRunsQuery
{
    public int? ProjectId { get; set; }
    public DevFlowRunStatus? Status { get; set; }
    public DevFlowStage? Stage { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Request para crear un DevFlow Run.
/// </summary>
public class CreateDevFlowRunRequest
{
    public int? ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta de un DevFlow Run creado.
/// </summary>
public class DevFlowRunResponse
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DevFlowRunStatus Status { get; set; }
    public DevFlowStage? CurrentStage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Estado de aprobación de un gate.
/// </summary>
public enum DevFlowGateStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// Respuesta detallada de un DevFlow Run con artifacts y gates.
/// </summary>
public class DevFlowRunDetailResponse
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DevFlowRunStatus Status { get; set; }
    public DevFlowStage? CurrentStage { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DevFlowArtifactSummaryDto> Artifacts { get; set; } = new();
    public List<DevFlowGateSummaryDto> Gates { get; set; } = new();
}

/// <summary>
/// Resumen de un artifact.
/// </summary>
public class DevFlowArtifactSummaryDto
{
    public int Id { get; set; }
    public DevFlowStage Stage { get; set; }
    public AgentRole AgentRole { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Resumen de un gate.
/// </summary>
public class DevFlowGateSummaryDto
{
    public int Id { get; set; }
    public DevFlowStage Stage { get; set; }
    public DevFlowGateStatus Status { get; set; }
    public string? DecisionComment { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request para ejecutar una etapa.
/// </summary>
public class ExecuteStageRequest
{
    public string? InputText { get; set; }
    public DevFlowStage? OverrideStage { get; set; }
}

/// <summary>
/// Respuesta de ejecución de etapa.
/// </summary>
public class ExecuteStageResponse
{
    public DevFlowRunDetailResponse Run { get; set; } = null!;
    public ExecuteStageArtifactDto Artifact { get; set; } = null!;
}

/// <summary>
/// Artefacto creado en la ejecución.
/// </summary>
public class ExecuteStageArtifactDto
{
    public int Id { get; set; }
    public DevFlowStage Stage { get; set; }
    public AgentRole AgentRole { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request para aprobar/rechazar gate.
/// </summary>
public class ApproveGateRequest
{
    public DevFlowStage Stage { get; set; }
    public bool Approved { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// Respuesta de aprobación/rechazo de gate.
/// </summary>
public class ApproveGateResponse
{
    public DevFlowGateSummaryDto Gate { get; set; } = null!;
    public DevFlowRunStatus Status { get; set; }
    public DevFlowStage? CurrentStage { get; set; }
}
