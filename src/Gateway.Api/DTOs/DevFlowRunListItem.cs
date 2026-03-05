using Data.Models;

namespace Gateway.Api.DTOs;

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
