namespace Data.Models;

public class Empresa
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<EmpresaUser> EmpresaUsers { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
}

