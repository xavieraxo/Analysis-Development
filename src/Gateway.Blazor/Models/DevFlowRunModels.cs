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
