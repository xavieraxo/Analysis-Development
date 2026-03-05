namespace Data.Models;

/// <summary>
/// Plan de ramas asociado a un DevFlowRun (1:1).
/// Contiene los ítems de tareas con nombres de rama sugeridos.
/// </summary>
public class BranchPlan
{
    public int Id { get; set; }
    public int DevFlowRunId { get; set; }
    public DevFlowRun DevFlowRun { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public int FormatVersion { get; set; } = 1;

    public List<BranchPlanItem> Items { get; set; } = new();
}
