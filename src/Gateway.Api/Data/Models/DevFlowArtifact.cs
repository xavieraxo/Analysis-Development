using Shared.Abstractions;

namespace Data.Models;

/// <summary>
/// Artefacto generado por un agente durante una etapa del flujo DevFlow.
/// Almacena el output JSON de cada etapa (UR, PM, PO, DEV).
/// </summary>
public class DevFlowArtifact
{
    public int Id { get; set; }
    public int DevFlowRunId { get; set; }
    public DevFlowRun DevFlowRun { get; set; } = null!;
    public DevFlowStage Stage { get; set; }
    public AgentRole AgentRole { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
