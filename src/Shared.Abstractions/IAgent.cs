namespace Shared.Abstractions;

public interface IAgent
{
    AgentRole Role { get; }
    Task<ChatMessage> HandleAsync(AgentTurn turn);
}

