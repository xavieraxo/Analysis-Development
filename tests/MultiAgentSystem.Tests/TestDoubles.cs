using System.Collections.Generic;
using System.Linq;
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

internal sealed class FakeBehaviorProvider : IBehaviorProvider
{
    private readonly BehaviorProfile _profile;

    public FakeBehaviorProvider(string prompt = "prompt-fake")
    {
        _profile = new BehaviorProfile
        {
            Role = AgentRole.Dev,
            Alias = "alias-fake",
            Prompt = prompt,
            Instructions = new List<BehaviorInstruction>(),
            FromFallback = false
        };
    }

    public Task<BehaviorProfile> GetBehaviorAsync(AgentRole role, CancellationToken ct = default)
    {
        var clone = new BehaviorProfile
        {
            Role = role,
            Alias = _profile.Alias,
            Prompt = _profile.Prompt,
            Instructions = _profile.Instructions.ToList(),
            FromFallback = _profile.FromFallback
        };
        return Task.FromResult(clone);
    }
}