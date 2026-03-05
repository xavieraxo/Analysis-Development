using Data.Models;

namespace Gateway.Api.DTOs;

/// <summary>
/// Input mínimo para ejecutar un agente en una etapa del DevFlow.
/// </summary>
public record DevFlowAgentInput
{
    public int RunId { get; init; }
    public int? ProjectId { get; init; }
    public string ConversationId { get; init; } = string.Empty;
    public string UserMessage { get; init; } = string.Empty;
    public string PreviousArtifactsSummary { get; init; } = string.Empty;
}
