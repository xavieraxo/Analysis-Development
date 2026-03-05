using Data.Models;
using Shared.Abstractions;

namespace Gateway.Api.DTOs;

/// <summary>
/// Respuesta de ejecución de etapa del DevFlow.
/// </summary>
public class ExecuteStageResponse
{
    public DevFlowRunDetailResponse Run { get; set; } = null!;
    public ExecuteStageArtifactDto Artifact { get; set; } = null!;
}

/// <summary>
/// DTO del artefacto creado en la ejecución.
/// </summary>
public class ExecuteStageArtifactDto
{
    public int Id { get; set; }
    public DevFlowStage Stage { get; set; }
    public AgentRole AgentRole { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}
