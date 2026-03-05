using Data.Models;
using Shared.Abstractions;

namespace Gateway.Api.DTOs;

/// <summary>
/// Resultado de la ejecución de un agente en una etapa del DevFlow.
/// </summary>
public record DevFlowAgentResult
{
    public AgentRole AgentRole { get; init; }
    public DevFlowStage Stage { get; init; }
    public string PayloadJson { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
    public Dictionary<string, string>? Metadata { get; init; }
}
