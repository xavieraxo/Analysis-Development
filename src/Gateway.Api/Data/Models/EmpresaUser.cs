namespace Data.Models;

public class EmpresaUser
{
    public int EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? RoleInEmpresa { get; set; }
}

