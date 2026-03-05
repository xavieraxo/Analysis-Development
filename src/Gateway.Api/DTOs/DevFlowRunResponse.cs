using Data.Models;

namespace Gateway.Api.DTOs;

/// <summary>
/// Respuesta de un DevFlow Run.
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
