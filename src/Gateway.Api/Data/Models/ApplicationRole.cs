using Microsoft.AspNetCore.Identity;

namespace Data.Models;

/// <summary>
/// Rol de la aplicación para ASP.NET Core Identity
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}

