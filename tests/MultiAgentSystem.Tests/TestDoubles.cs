using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shared.Abstractions;

namespace MultiAgentSystem.Tests;

internal sealed class FakeLlmClient : ILlmClient
{
    private readonly Queue<string> _responses;

    public FakeLlmClient(params string[] responses)
    {
        _responses = new Queue<string>(responses.Length > 0 ? responses : new[] { "respuesta-fake" });
    }

    public List<(string system, string user)> Calls { get; } = new();

    public Task<string> CompleteAsync(string system, string user, CancellationToken ct)
    {
        Calls.Add((system, user));
        var response = _responses.Count > 0 ? _responses.Dequeue() : "respuesta-fake";
        return Task.FromResult(response);
    }
}

internal sealed class TestAgent : IAgent
{
    private readonly Queue<string> _responses;

    public TestAgent(AgentRole role, params string[] responses)
    {
        Role = role;
        _responses = new Queue<string>(responses.Length > 0 ? responses : new[] { $"respuesta-{role}" });
    }

    public AgentRole Role { get; }

    public List<AgentTurn> Calls { get; } = new();

    public Task<ChatMessage> HandleAsync(AgentTurn turn)
    {
        Calls.Add(turn);
        var response = _responses.Count > 0 ? _responses.Dequeue() : $"respuesta-{Role}";
        var message = new ChatMessage(turn.ConversationId, Role, response, DateTimeOffset.UtcNow);
        return Task.FromResult(message);
    }
}
