using Shared.Abstractions;

namespace Orchestrator.App;

public interface ILoggingCallback
{
    void LogAgentInteraction(string conversationId, AgentRole agentRole, string message, string? context = null);
}

public sealed class Orchestrator
{
    private readonly IEnumerable<IAgent> _agents;
    private readonly ILlmClient _llm;
    private readonly ILoggingCallback? _loggingCallback;

    public Orchestrator(IEnumerable<IAgent> agents, ILlmClient llm, ILoggingCallback? loggingCallback = null)
    {
        _agents = agents;
        _llm = llm;
        _loggingCallback = loggingCallback;
    }

    public async IAsyncEnumerable<ChatMessage> RunAsync(
        string conversationId,
        ChatMessage userMessage,
        AgentRole[] flow,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1) Normaliza/valida mensaje del usuario
        var ctx = userMessage;

        // 2) Itera el flujo (p.ej., PM -> DEV -> PM)
        foreach (var step in flow)
        {
            var agent = _agents.First(a => a.Role == step);
            _loggingCallback?.LogAgentInteraction(conversationId, step, $"Iniciando procesamiento...", ctx.Text);
            
            var msg = await agent.HandleAsync(new AgentTurn(conversationId, step, ctx, ct));
            
            _loggingCallback?.LogAgentInteraction(conversationId, step, msg.Text, $"Contexto previo: {ctx.Text}");
            
            yield return msg;          // streaming para el front
            ctx = msg;                 // siguiente agente ve el output previo
        }
    }

    /// <summary>
    /// Ejecuta el flujo completo y genera un resumen final consolidado
    /// </summary>
    public async Task<ChatResponse> RunWithSummaryAsync(
        string conversationId,
        ChatMessage userMessage,
        AgentRole[] flow,
        CancellationToken ct = default)
    {
        // Ejecutar todas las conversaciones internas
        var internalMessages = new List<ChatMessage>();
        var ctx = userMessage;

        foreach (var step in flow)
        {
            var agent = _agents.First(a => a.Role == step);
            _loggingCallback?.LogAgentInteraction(conversationId, step, $"Iniciando procesamiento...", ctx.Text);
            
            var msg = await agent.HandleAsync(new AgentTurn(conversationId, step, ctx, ct));
            
            _loggingCallback?.LogAgentInteraction(conversationId, step, msg.Text, $"Contexto previo: {ctx.Text}");
            
            internalMessages.Add(msg);
            ctx = msg;
        }

        // Generar resumen final consolidado usando el PM
        var pmAgent = _agents.First(a => a.Role == AgentRole.PM);
        var summaryPrompt = $"Solicitud del usuario: {userMessage.Text}\n\n" +
                           $"Análisis interno realizado:\n" +
                           string.Join("\n\n", internalMessages.Select((m, i) => 
                               $"{m.From}: {m.Text}")) +
                           $"\n\nTarea: Genera un resumen ejecutivo del MVP que incluya:\n" +
                           $"- Composición del MVP (qué incluye)\n" +
                           $"- Arquitectura y tecnologías propuestas\n" +
                           $"- Componentes principales\n" +
                           $"- Sugerencias adicionales si las hay\n" +
                           $"- Formato claro y estructurado para el usuario final";

        var summaryText = await _llm.CompleteAsync(
            "Eres un Project Manager experto. Genera resúmenes ejecutivos claros, estructurados y profesionales para usuarios finales.",
            summaryPrompt,
            ct);

        var summary = new ChatMessage(
            conversationId,
            AgentRole.PM,
            summaryText,
            DateTimeOffset.UtcNow,
            new Dictionary<string, string> { { "type", "summary" } });

        return new ChatResponse(summary, internalMessages);
    }
}

