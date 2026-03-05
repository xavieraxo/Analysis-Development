namespace Data.Models;

/// <summary>
/// Gate de aprobación humana entre etapas del flujo DevFlow.
/// Registra la decisión (approve/reject), quién la tomó y cuándo.
/// </summary>
public class DevFlowGate
{
    public int Id { get; set; }
    public int DevFlowRunId { get; set; }
    public DevFlowRun DevFlowRun { get; set; } = null!;
    public DevFlowStage Stage { get; set; }
    public DevFlowGateStatus Status { get; set; } = DevFlowGateStatus.Pending;
    public string? DecisionComment { get; set; }
    public int? DecidedByUserId { get; set; }
    public ApplicationUser? DecidedByUser { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
