namespace Data.Models;

/// <summary>
/// Ítem de tarea dentro de un BranchPlan (rama sugerida por tarea).
/// </summary>
public class BranchPlanItem
{
    public int Id { get; set; }
    public int BranchPlanId { get; set; }
    public BranchPlan BranchPlan { get; set; } = null!;

    public string StoryId { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
