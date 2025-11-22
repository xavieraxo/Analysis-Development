namespace Data.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpires { get; set; }
    
    // Relaciones
    public List<Project> Projects { get; set; } = new();
}

public enum UserRole
{
    Final = 0,      // Usuario final
    Empresa = 1,    // Usuario empresa
    Admin = 2,      // Administrador
    SuperUsuario = 3 // Super usuario (acceso con hash)
}

