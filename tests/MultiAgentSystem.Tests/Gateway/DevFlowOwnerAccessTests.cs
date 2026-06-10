using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Data.Models;
using Gateway.Api.DTOs;
using Gateway.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests de la política de acceso por ownership a DevFlow runs (tarea 10.2.1, PLAN_DISCOVERY_MVP).
/// El dueño del proyecto consulta su run y la lista de runs de su proyecto; los runs ajenos
/// devuelven 404; los endpoints de gestión siguen siendo SuperUsuario-only.
/// Usa un usuario dedicado propio (distinto del de CreateProjectWithDiscoveryTests) para no
/// competir por el límite de "un proyecto activo" en ejecuciones paralelas.
/// </summary>
public class DevFlowOwnerAccessTests :
    IClassFixture<GatewayApiFactoryOwnerUser>,
    IClassFixture<GatewayApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly GatewayApiFactoryOwnerUser _ownerFactory;
    private readonly GatewayApiFactory _superUserFactory;

    public DevFlowOwnerAccessTests(GatewayApiFactoryOwnerUser ownerFactory, GatewayApiFactory superUserFactory)
    {
        _ownerFactory = ownerFactory;
        _superUserFactory = superUserFactory;
    }

    [Fact]
    public async Task GetRun_DueñoDelProyecto_Devuelve200()
    {
        var (ownerClient, _) = await CreateOwnerClientAsync();
        var run = await CreateOwnProjectWithDiscoveryAsync(ownerClient);

        var response = await ownerClient.GetAsync($"/api/devflow/runs/{run.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<DevFlowRunDetailResponse>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(run.Id, detail.Id);
        Assert.Equal(DevFlowType.Discovery, detail.FlowType);
    }

    [Fact]
    public async Task GetRun_RunAjeno_Devuelve404()
    {
        var (ownerClient, _) = await CreateOwnerClientAsync();

        // Run de otro usuario (el SuperUsuario id 1 crea proyecto + run por la vía admin).
        var superClient = _superUserFactory.CreateClient();
        var foreignProjectId = await GatewayTestHelpers.CreateProjectAsync(superClient);
        var createRun = await superClient.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            ProjectId = foreignProjectId,
            Title = "Run ajeno",
            Description = "No debe ser visible para otro usuario",
            FlowType = DevFlowType.Discovery
        });
        Assert.Equal(HttpStatusCode.Created, createRun.StatusCode);
        var foreignRun = await createRun.Content.ReadFromJsonAsync<DevFlowRunResponse>(JsonOptions);
        Assert.NotNull(foreignRun);

        var response = await ownerClient.GetAsync($"/api/devflow/runs/{foreignRun.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRun_SuperUsuario_RunDeOtroUsuario_Devuelve200()
    {
        var (ownerClient, _) = await CreateOwnerClientAsync();
        var run = await CreateOwnProjectWithDiscoveryAsync(ownerClient);

        var superClient = _superUserFactory.CreateClient();
        var response = await superClient.GetAsync($"/api/devflow/runs/{run.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExecuteStage_DueñoSinRolSuperUsuario_Devuelve403()
    {
        var (ownerClient, _) = await CreateOwnerClientAsync();
        var run = await CreateOwnProjectWithDiscoveryAsync(ownerClient);

        var response = await ownerClient.PostAsJsonAsync($"/api/devflow/runs/{run.Id}/execute-stage", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApproveGate_DueñoSinRolSuperUsuario_Devuelve403()
    {
        var (ownerClient, _) = await CreateOwnerClientAsync();
        var run = await CreateOwnProjectWithDiscoveryAsync(ownerClient);

        var response = await ownerClient.PostAsJsonAsync($"/api/devflow/runs/{run.Id}/approve", new
        {
            Stage = "UR",
            Approved = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectRuns_DueñoDelProyecto_Devuelve200ConSuRun()
    {
        var (ownerClient, _) = await CreateOwnerClientAsync();
        var run = await CreateOwnProjectWithDiscoveryAsync(ownerClient);

        var response = await ownerClient.GetAsync($"/api/projects/{run.ProjectId}/devflow-runs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<DevFlowRunListItem>>(JsonOptions);
        Assert.NotNull(paged);
        Assert.Contains(paged.Items, r => r.Id == run.Id);
    }

    [Fact]
    public async Task GetProjectRuns_ProyectoAjeno_Devuelve404()
    {
        var (ownerClient, _) = await CreateOwnerClientAsync();

        var superClient = _superUserFactory.CreateClient();
        var foreignProjectId = await GatewayTestHelpers.CreateProjectAsync(superClient);

        var response = await ownerClient.GetAsync($"/api/projects/{foreignProjectId}/devflow-runs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(HttpClient Client, int UserId)> CreateOwnerClientAsync()
    {
        var userId = await GatewayTestHelpers.EnsureDedicatedUserWithoutActiveProjectsAsync(
            _ownerFactory.Services, OwnerTestUser.Email, "Usuario Owner Tests");
        OwnerTestUser.Id = userId;
        return (_ownerFactory.CreateClient(), userId);
    }

    private static async Task<DevFlowRunResponse> CreateOwnProjectWithDiscoveryAsync(HttpClient ownerClient)
    {
        var response = await ownerClient.PostAsJsonAsync("/api/projects/with-discovery", new
        {
            Name = $"Proyecto owner {Guid.NewGuid():N}",
            Description = "Proyecto para tests de ownership"
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(HttpStatusCode.Created == response.StatusCode, $"Status: {response.StatusCode}. Body: {body}");
        var result = JsonSerializer.Deserialize<ProjectWithDevFlowDto>(body, JsonOptions);
        Assert.NotNull(result);
        return result.InitialRun;
    }
}

/// <summary>
/// Identidad del usuario dedicado a los tests de ownership.
/// </summary>
public static class OwnerTestUser
{
    public const string Email = "devflow-owner-tests@system.local";
    public static int Id;
}

/// <summary>
/// Factory con autenticación del usuario dueño (rol Final, sin privilegios de administración).
/// </summary>
public class GatewayApiFactoryOwnerUser : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestOwner";
                options.DefaultChallengeScheme = "TestOwner";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandlerOwnerUser>("TestOwner", _ => { });
        });
    }
}

public class TestAuthHandlerOwnerUser : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandlerOwnerUser(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, OwnerTestUser.Id.ToString()),
            new Claim(ClaimTypes.Role, "Final"),
            new Claim(ClaimTypes.Email, OwnerTestUser.Email)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
