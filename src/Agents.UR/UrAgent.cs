using System.Reflection;
using System.Text;
using Shared.Abstractions;

namespace Agents.UR;

public sealed class UrAgent : IAgent
{
    private readonly ILlmClient _llm;
    private readonly string _systemPrompt;

    public UrAgent(ILlmClient llm)
    {
        _llm = llm;
        _systemPrompt = LoadSystemPrompt();
    }

    public AgentRole Role => AgentRole.UR;

    private string LoadSystemPrompt()
    {
        const string defaultPrompt = "Eres el Usuario Representante. Actúas como puente entre el cliente real y los agentes internos. Tu misión es hacer preguntas claras al cliente para obtener información validada, sin inventar ni asumir nada.";
        const string promptFileName = "promptUsuarioRepresentante.md";

        try
        {
            // 1. Intentar leer desde el directorio de ejecución (bin/Debug o bin/Release)
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(assemblyDirectory))
                {
                    var promptPath = Path.Combine(assemblyDirectory, promptFileName);
                    if (File.Exists(promptPath))
                    {
                        return File.ReadAllText(promptPath, Encoding.UTF8);
                    }
                }
            }

            // 2. Intentar desde AppDomain.CurrentDomain.BaseDirectory (útil en diferentes contextos)
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                var basePromptPath = Path.Combine(baseDirectory, promptFileName);
                if (File.Exists(basePromptPath))
                {
                    return File.ReadAllText(basePromptPath, Encoding.UTF8);
                }
            }

            // 3. Intentar desde el directorio del proyecto (desarrollo)
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var projectDirectory = Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(projectDirectory))
                {
                    // Navegar hacia arriba desde bin/Debug/net9.0 hasta el proyecto
                    var projectPromptPath = Path.GetFullPath(
                        Path.Combine(projectDirectory, "..", "..", "..", promptFileName));
                    
                    if (File.Exists(projectPromptPath))
                    {
                        return File.ReadAllText(projectPromptPath, Encoding.UTF8);
                    }
                }
            }

            // 4. Fallback final: prompt por defecto
            return defaultPrompt;
        }
        catch (Exception ex)
        {
            // Log del error (en producción se podría usar ILogger)
            Console.WriteLine($"[UrAgent] Error al cargar promptUsuarioRepresentante.md: {ex.Message}");
            return defaultPrompt;
        }
    }

    public async Task<ChatMessage> HandleAsync(AgentTurn turn)
    {
        var prompt = $"Contexto recibido: {turn.Context.Text}\n\n" +
                    $"Tarea: Analiza la solicitud del cliente y determina qué información necesitas validar con él. " +
                    $"Si hay ambigüedades, dudas o información faltante, formula preguntas claras y no técnicas al cliente. " +
                    $"Si la información es suficiente para iniciar el discovery, confirma lo entendido y prepara las preguntas clave que los otros agentes necesitarán. " +
                    $"NUNCA inventes, asumas ni completes información. Solo el cliente puede decidir lo que quiere.";
        
        var text = await _llm.CompleteAsync(_systemPrompt, prompt, turn.Ct);
        return new ChatMessage(turn.ConversationId, Role, text, DateTimeOffset.UtcNow);
    }
}

