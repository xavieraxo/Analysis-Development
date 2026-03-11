namespace Data.Models;

public class ProjectOperator
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}

