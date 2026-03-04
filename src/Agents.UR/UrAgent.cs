using System.Text;
using System.Linq;
using Shared.Abstractions;

namespace Agents.UR;

public sealed class UrAgent : IAgent
{
    private readonly ILlmClient _llm;
    private readonly IBehaviorProvider _behaviorProvider;

    public UrAgent(ILlmClient llm, IBehaviorProvider behaviorProvider)
    {
        _llm = llm;
        _behaviorProvider = behaviorProvider;
    }

    public AgentRole Role => AgentRole.UR;

    public async Task<ChatMessage> HandleAsync(AgentTurn turn)
    {
        var behavior = await _behaviorProvider.GetBehaviorAsync(Role, turn.Ct);
        var systemPrompt = BuildSystemPrompt(behavior);
        var prompt = $"Contexto recibido: {turn.Context.Text}\n\n" +
                    $"Tarea: Analiza la solicitud del cliente y determina qué información necesitas validar con él. " +
                    $"Si hay ambigüedades, dudas o información faltante, formula preguntas claras y no técnicas al cliente. " +
                    $"Si la información es suficiente para iniciar el discovery, confirma lo entendido y prepara las preguntas clave que los otros agentes necesitarán. " +
                    $"NUNCA inventes, asumas ni completes información. Solo el cliente puede decidir lo que quiere.";
        
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

