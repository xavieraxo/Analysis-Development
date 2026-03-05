using System.Text;
using System.Linq;
using Shared.Abstractions;

namespace Agents.PO;

public sealed class PoAgent : IAgent
{
    private readonly ILlmClient _llm;
    private readonly IBehaviorProvider _behaviorProvider;

    public PoAgent(ILlmClient llm, IBehaviorProvider behaviorProvider)
    {
        _llm = llm;
        _behaviorProvider = behaviorProvider;
    }

    public AgentRole Role => AgentRole.PO;

    public async Task<ChatMessage> HandleAsync(AgentTurn turn)
    {
        var behavior = await _behaviorProvider.GetBehaviorAsync(Role, turn.Ct);
        var systemPrompt = BuildSystemPrompt(behavior);
        var prompt = $"Contexto: {turn.Context.Text}\nTarea: Define el problema, usuario, valor y funcionalidades del MVP desde la perspectiva del producto.";
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

