using System.Reflection;
using System.Text;
using Shared.Abstractions;

namespace Agents.UX;

public sealed class UxAgent : IAgent
{
    private readonly ILlmClient _llm;
    private readonly string _systemPrompt;

    public UxAgent(ILlmClient llm)
    {
        _llm = llm;
        _systemPrompt = LoadSystemPrompt();
    }

    public AgentRole Role => AgentRole.UX;

    private string LoadSystemPrompt()
    {
        const string defaultPrompt = "Eres un experto en UX/UI + Frontend. Diseñas experiencias de usuario, interfaces visuales y aportas visión técnica de frontend.";
        const string promptFileName = "promptAgenteUX-UI.md";

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
            Console.WriteLine($"[UxAgent] Error al cargar promptAgenteUX-UI.md: {ex.Message}");
            return defaultPrompt;
        }
    }

    public async Task<ChatMessage> HandleAsync(AgentTurn turn)
    {
        var prompt = $"Contexto: {turn.Context.Text}\nTarea: Diseña flujos de usuario, wireframes conceptuales, componentes de UI y aporta visión técnica de frontend dentro de las restricciones del DEV y objetivos del PO.";
        var text = await _llm.CompleteAsync(_systemPrompt, prompt, turn.Ct);
        return new ChatMessage(turn.ConversationId, Role, text, DateTimeOffset.UtcNow);
    }
}

