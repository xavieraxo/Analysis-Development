using System.Text;
using System.Linq;
using Shared.Abstractions;

namespace Agents.Dev;

public sealed class DevAgent : IAgent
{
    private readonly ILlmClient _llm;
    private readonly IBehaviorProvider _behaviorProvider;

    public DevAgent(ILlmClient llm, IBehaviorProvider behaviorProvider)
    {
        _llm = llm;
        _behaviorProvider = behaviorProvider;
    }

    public AgentRole Role => AgentRole.Dev;

    public async Task<ChatMessage> HandleAsync(AgentTurn turn)
    {
        var behavior = await _behaviorProvider.GetBehaviorAsync(Role, turn.Ct);
        var systemPrompt = BuildSystemPrompt(behavior);
        var prompt = $"Requisitos: {turn.Context.Text}\nEntrega: diagrama, APIs, snippet C# minimal viable.";
        var text = await _llm.CompleteAsync(systemPrompt, prompt, turn.Ct);
        return new ChatMessage(turn.ConversationId, Role, text, DateTimeOffset.UtcNow);
    }

    private static string BuildSystemPrompt(BehaviorProfile behavior)
    {
        var sb = new StringBuilder();
        sb.AppendLine(behavior.Prompt);
        if (behavior.Instructions.Any())
        {
            sb.AppendLine("Instrucciones específicas:");
            foreach (var instruction in behavior.Instructions)
            {
                sb.AppendLine($"P: {instruction.Question}");
                sb.AppendLine($"R: {instruction.Answer}");
            }
        }
        return sb.ToString();
    }
}

