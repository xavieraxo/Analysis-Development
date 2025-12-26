using Microsoft.AspNetCore.Identity;

namespace Data.Models;

/// <summary>
/// Usuario de la aplicación que extiende IdentityUser para agregar campos personalizados.
/// Coexiste con la tabla User durante la migración gradual a Identity.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    // Campos personalizados adicionales a IdentityUser
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Mantener compatibilidad con User antiguo durante la migración
    public int? LegacyUserId { get; set; }
    
    // Relaciones con otras entidades (se migrará en fase final)
    // public List<Project> Projects { get; set; } = new();
}

