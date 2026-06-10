namespace Data.Models;

/// <summary>
/// Emisor de un mensaje dentro de una etapa conversacional del DevFlow.
/// </summary>
public enum StageMessageSender
{
    User = 0,
    Agent = 1
}

/// <summary>
/// Mensaje de la conversación de una etapa de un DevFlow run (tarea 11.1.1, PLAN_DISCOVERY_MVP).
/// Genérico por etapa y tipo de flujo: en Etapa 1 lo usa la etapa UR del Discovery,
/// pero AutoDev/Development pueden reutilizarlo sin cambios (requisito Etapa 2).
/// </summary>
public class DevFlowStageMessage
{
    public int Id { get; set; }
    public int DevFlowRunId { get; set; }
    public DevFlowRun DevFlowRun { get; set; } = null!;
    public DevFlowStage Stage { get; set; }
    public StageMessageSender Sender { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
