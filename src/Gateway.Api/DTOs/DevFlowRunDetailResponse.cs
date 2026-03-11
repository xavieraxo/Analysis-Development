using Data.Models;
using Shared.Abstractions;

namespace Gateway.Api.DTOs;

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
    public DevFlowScope Scope { get; set; }
    public List<DevFlowArtifactSummaryDto> Artifacts { get; set; } = new();
    public List<DevFlowGateSummaryDto> Gates { get; set; } = new();
}

/// <summary>
/// Resumen de un artifact (sin PayloadJson para evitar payload grande).
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
