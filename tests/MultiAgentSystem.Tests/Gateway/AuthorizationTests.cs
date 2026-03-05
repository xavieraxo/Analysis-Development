using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Verifica que solo SuperUsuario puede acceder a endpoints internos.
/// </summary>
public class SuperUserAuthorizationTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;

    public SuperUserAuthorizationTests(GatewayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetConfigurations_ConSuperUsuario_Devuelve200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/configurations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBehaviors_ConSuperUsuario_Devuelve200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/behaviors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminUsers_ConSuperUsuario_Devuelve200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

/// <summary>
/// Verifica que Admin recibe 403 en endpoints internos.
/// </summary>
public class AdminForbiddenTests : IClassFixture<GatewayApiFactoryAdmin>
{
    private readonly GatewayApiFactoryAdmin _factory;

    public AdminForbiddenTests(GatewayApiFactoryAdmin factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetConfigurations_ConAdmin_Devuelve403()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/configurations");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBehaviors_ConAdmin_Devuelve403()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/behaviors");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminUsers_ConAdmin_Devuelve403()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public class GatewayApiFactoryAdmin : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAgent>();
            services.RemoveAll<ILlmClient>();
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandlerAdmin>("Test", _ => { });

            services.AddSingleton<IAgent>(new TestAgent(AgentRole.PM, "Plan test", "Cierre test"));
            services.AddSingleton<IAgent>(new TestAgent(AgentRole.Dev, "Implementación test"));
            services.AddSingleton<IAgent>(new TestAgent(AgentRole.UR, "UR test"));
            services.AddSingleton<IAgent>(new TestAgent(AgentRole.PO, "PO test"));
            services.AddSingleton<IAgent>(new TestAgent(AgentRole.UX, "UX test"));
            services.AddSingleton<ILlmClient>(new FakeLlmClient());
        });
    }
}
