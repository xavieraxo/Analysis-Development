using System.Net;
using System.Net.Http.Json;
using Data;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MultiAgentSystem.Tests.Gateway;

internal static class GatewayTestHelpers
{
    /// <summary>
    /// Seedea (si no existe) un usuario dedicado de tests en IdentityUsers y Users (legacy)
    /// con el mismo id, y completa sus proyectos activos para que el límite de "un proyecto
    /// activo" parta de estado conocido. La BD de integración es persistente y compartida.
    /// Nota: DevFlowRuns.CreatedByUserId tiene FK a IdentityUsers mientras Projects.UserId
    /// referencia a Users (legacy); el sistema asume ids alineados entre ambas tablas.
    /// </summary>
    public static async Task<int> EnsureDedicatedUserWithoutActiveProjectsAsync(IServiceProvider services, string email, string name)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var identityUser = await db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == email);
        if (identityUser == null)
        {
            identityUser = new ApplicationUser
            {
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                Name = name,
                Role = UserRole.Final,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };
            db.ApplicationUsers.Add(identityUser);
            await db.SaveChangesAsync();
        }

        var legacyUser = await db.Users.FirstOrDefaultAsync(u => u.Id == identityUser.Id);
        if (legacyUser == null)
        {
            legacyUser = new User
            {
                Id = identityUser.Id,
                Email = email,
                Name = name,
                Role = UserRole.Final,
                IsActive = true,
                PasswordHash = "integration-test-only"
            };
            db.Users.Add(legacyUser);
            await db.SaveChangesAsync();
        }

        var activos = await db.Projects
            .Where(p => p.UserId == identityUser.Id &&
                        (p.Status == ProjectStatus.InProgress || p.Status == ProjectStatus.OnHold))
            .ToListAsync();

        foreach (var proyecto in activos)
        {
            proyecto.Status = ProjectStatus.Completed;
            proyecto.CompletedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return identityUser.Id;
    }

    public static async Task<int> CreateProjectAsync(HttpClient client, string? name = null, string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            Name = name ?? $"Proyecto {Guid.NewGuid():N}",
            Description = description ?? "Proyecto de prueba para DevFlow"
        });

        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.Created)
            throw new InvalidOperationException($"No se pudo crear proyecto de prueba. Status: {response.StatusCode}. Body: {body}");

        var project = await response.Content.ReadFromJsonAsync<TestProjectDto>();
        if (project is null)
            throw new InvalidOperationException("La respuesta de creación de proyecto fue nula.");

        return project.Id;
    }

    private sealed class TestProjectDto
    {
        public int Id { get; set; }
    }
}
