using Data.Models;
using Gateway.Api.DTOs;
using Shared.Abstractions;

namespace Gateway.Api.Services;

/// <summary>
/// Implementación del despachador de agentes para DevFlow.
/// Usa IDevFlowPipeline para stage→AgentRole y resuelve el agente desde DI.
/// </summary>
public sealed class DevFlowAgentDispatcher : IDevFlowAgentDispatcher
{
    private readonly IDevFlowPipeline _pipeline;
    private readonly IEnumerable<IAgent> _agents;

    public DevFlowAgentDispatcher(IDevFlowPipeline pipeline, IEnumerable<IAgent> agents)
    {
        _pipeline = pipeline;
        _agents = agents;
    }

    /// <inheritdoc />
    public IAgent ResolveAgent(DevFlowStage stage)
    {
        var role = _pipeline.GetAgentRoleForStage(stage);
        var agent = _agents.FirstOrDefault(a => a.Role == role);
        if (agent is null)
            throw new InvalidOperationException($"No hay agente registrado para el stage {stage} (rol {role}).");

        return agent;
    }

    /// <inheritdoc />
    public async Task<DevFlowAgentResult> ExecuteAsync(
        DevFlowStage stage,
        DevFlowAgentInput input,
        CancellationToken cancellationToken = default)
    {
        var agent = ResolveAgent(stage);
        var role = _pipeline.GetAgentRoleForStage(stage);

        var contextText = input.UserMessage;
        if (!string.IsNullOrWhiteSpace(input.PreviousArtifactsSummary))
            contextText = $"{input.UserMessage}\n\nResumen de artefactos previos:\n{input.PreviousArtifactsSummary}";

        var conversationId = !string.IsNullOrWhiteSpace(input.ConversationId)
            ? input.ConversationId
            : $"devflow-{input.RunId}";

        var context = new ChatMessage(
            conversationId,
            AgentRole.User,
            contextText,
            DateTimeOffset.UtcNow);

        var turn = new AgentTurn(conversationId, role, context, cancellationToken);
        var message = await agent.HandleAsync(turn);

        return new DevFlowAgentResult
        {
            AgentRole = role,
            Stage = stage,
            PayloadJson = message.Text,
            Version = 1,
            Metadata = message.Meta is not null ? new Dictionary<string, string>(message.Meta) : null
        };
    }
}
