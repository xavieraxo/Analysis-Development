namespace Shared.Abstractions;

public record AgentTurn(
    string ConversationId,
    AgentRole Target,
    ChatMessage Context,
    CancellationToken Ct);

