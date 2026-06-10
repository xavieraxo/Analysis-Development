using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Data;
using Data.Models;
using Gateway.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests de integración para POST /api/projects/with-discovery (tarea 10.1.1, PLAN_DISCOVERY_MVP).
/// Usa un usuario dedicado (no el id 1 compartido) porque la BD es persistente y el resto de la
/// suite crea proyectos activos para el usuario 1 en paralelo, lo que haría no determinista el
/// límite de "un proyecto activo".
/// </summary>
public class CreateProjectWithDiscoveryTests : IClassFixture<GatewayApiFactoryDiscoveryUser>
{
    // El API serializa enums como string (JsonStringEnumConverter en Program.cs).
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly GatewayApiFactoryDiscoveryUser _factory;

    public CreateProjectWithDiscoveryTests(GatewayApiFactoryDiscoveryUser factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProjectWithDiscovery_UsuarioSinProyectoActivo_Devuelve201ConRunDiscovery()
    {
        var userId = await EnsureDedicatedUserWithoutActiveProjectsAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/projects/with-discovery", new
        {
            Name = $"Proyecto Discovery {Guid.NewGuid():N}",
            Description = "Descripción inicial del desarrollo deseado"
        });

        var rawBody = await response.Content.ReadAsStringAsync();
        Assert.True(HttpStatusCode.Created == response.StatusCode, $"Status: {response.StatusCode}. Body: {rawBody}");
        var result = JsonSerializer.Deserialize<ProjectWithDevFlowDto>(rawBody, JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.Project.Id > 0);
        Assert.Equal(userId, result.Project.UserId);
        Assert.Equal(ProjectStatus.InProgress, result.Project.Status);
        Assert.NotNull(result.InitialRun);
        Assert.True(result.InitialRun.Id > 0);
        Assert.Equal(result.Project.Id, result.InitialRun.ProjectId);
        Assert.Equal(DevFlowType.Discovery, result.InitialRun.FlowType);
        Assert.Equal(DevFlowRunStatus.Created, result.InitialRun.Status);
        Assert.Equal(DevFlowStage.UR, result.InitialRun.CurrentStage);
        Assert.Equal("Descripción inicial del desarrollo deseado", result.InitialRun.Description);
    }

    [Fact]
    public async Task CreateProjectWithDiscovery_ConProyectoActivo_Devuelve409()
    {
        await EnsureDedicatedUserWithoutActiveProjectsAsync();
        var client = _factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/projects/with-discovery", new
        {
            Name = $"Proyecto activo {Guid.NewGuid():N}",
            Description = "Primer proyecto"
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/projects/with-discovery", new
        {
            Name = $"Proyecto bloqueado {Guid.NewGuid():N}",
            Description = "Debe rechazarse por límite de proyecto activo"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateProjectWithDiscovery_SinNombre_Devuelve400()
    {
        await EnsureDedicatedUserWithoutActiveProjectsAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/projects/with-discovery", new
        {
            Name = "  ",
            Description = "Sin nombre válido"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Crea (si no existe) el usuario dedicado de esta suite y completa sus proyectos activos
    /// para que el límite de "un proyecto activo" parta de un estado conocido.
    /// Nota: DevFlowRuns.CreatedByUserId tiene FK a IdentityUsers mientras Projects.UserId
    /// referencia a Users (legacy); el sistema asume ids alineados entre ambas tablas, por lo
    /// que el usuario de test se seedea en las dos con el mismo id.
    /// </summary>
    private async Task<int> EnsureDedicatedUserWithoutActiveProjectsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var identityUser = await db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == DiscoveryTestUser.Email);
        if (identityUser == null)
        {
            identityUser = new ApplicationUser
            {
                UserName = DiscoveryTestUser.Email,
                NormalizedUserName = DiscoveryTestUser.Email.ToUpperInvariant(),
                Email = DiscoveryTestUser.Email,
                NormalizedEmail = DiscoveryTestUser.Email.ToUpperInvariant(),
                Name = "Usuario Discovery Tests",
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
                Email = DiscoveryTestUser.Email,
                Name = "Usuario Discovery Tests",
                Role = UserRole.Final,
                IsActive = true,
                PasswordHash = "integration-test-only"
            };
            db.Users.Add(legacyUser);
            await db.SaveChangesAsync();
        }

        DiscoveryTestUser.Id = identityUser.Id;

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
}

/// <summary>
/// Identidad del usuario dedicado a los tests de Discovery. El Id se resuelve al seedear
/// (la BD es persistente y el id lo genera PostgreSQL).
/// </summary>
public static class DiscoveryTestUser
{
    public const string Email = "discovery-mvp-tests@system.local";
    public static int Id;
}

/// <summary>
/// Factory con autenticación del usuario dedicado de Discovery (no el SuperUsuario id 1).
/// </summary>
public class GatewayApiFactoryDiscoveryUser : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestDiscovery";
                options.DefaultChallengeScheme = "TestDiscovery";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandlerDiscoveryUser>("TestDiscovery", _ => { });
        });
    }
}

public class TestAuthHandlerDiscoveryUser : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandlerDiscoveryUser(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, DiscoveryTestUser.Id.ToString()),
            new Claim(ClaimTypes.Role, "Final"),
            new Claim(ClaimTypes.Email, DiscoveryTestUser.Email)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
