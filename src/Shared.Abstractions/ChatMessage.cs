namespace Shared.Abstractions;

public record ChatMessage(
    string ConversationId,
    AgentRole From,
    string Text,
    DateTimeOffset At,
    Dictionary<string, string>? Meta = null);

