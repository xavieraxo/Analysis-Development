using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Gateway.Api.DTOs;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests de integración para GET /api/devflow/runs/{id}.
/// </summary>
public class GetDevFlowRunTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;

    public GetDevFlowRunTests(GatewayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDevFlowRun_ConSuperUsuario_RunExiste_Devuelve200()
    {
        var client = _factory.CreateClient();

        var createRequest = new CreateDevFlowRunRequest
        {
            Title = "Run para GET test",
            Description = "Descripción"
        };
        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        var getResponse = await client.GetAsync($"/api/devflow/runs/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var run = await getResponse.Content.ReadFromJsonAsync<DevFlowRunDetailResponse>();
        Assert.NotNull(run);
        Assert.Equal(created.Id, run.Id);
        Assert.Equal("Run para GET test", run.Title);
        Assert.NotNull(run.Artifacts);
        Assert.NotNull(run.Gates);
        Assert.Empty(run.Artifacts);
        Assert.Empty(run.Gates);
    }

    [Fact]
    public async Task GetDevFlowRun_ConSuperUsuario_RunNoExiste_Devuelve404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devflow/runs/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>
/// Verifica que Admin recibe 403 al obtener DevFlow run.
/// </summary>
public class GetDevFlowRunAdminForbiddenTests : IClassFixture<GatewayApiFactoryAdmin>
{
    private readonly GatewayApiFactoryAdmin _factory;

    public GetDevFlowRunAdminForbiddenTests(GatewayApiFactoryAdmin factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDevFlowRun_ConAdmin_Devuelve403()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devflow/runs/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
