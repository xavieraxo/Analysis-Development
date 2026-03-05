namespace Gateway.Api.DTOs;

/// <summary>
/// DTO para exportación del Branch Plan (JSON).
/// </summary>
public class BranchPlanExportDto
{
    public int RunId { get; set; }
    public int BranchPlanId { get; set; }
    public List<BranchPlanItemExportDto> Items { get; set; } = new();
}

/// <summary>
/// Ítem del plan de ramas para exportación.
/// </summary>
public class BranchPlanItemExportDto
{
    public int Order { get; set; }
    public string StoryId { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}
