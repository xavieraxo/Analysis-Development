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
        var projectId = await GatewayTestHelpers.CreateProjectAsync(client);

        var createRequest = new CreateDevFlowRunRequest
        {
            ProjectId = projectId,
            Title = "Run para GET test",
            Description = "Descripción",
            FlowType = Data.Models.DevFlowType.Discovery
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
        Assert.Equal(Data.Models.DevFlowType.Discovery, run.FlowType);
        Assert.NotNull(run.Artifacts);
        Assert.NotNull(run.Gates);
        Assert.Empty(run.Artifacts);
        Assert.Empty(run.Gates);
    }

    [Fact]
    public async Task GetDevFlowRun_ExponePayloadJsonDelArtifact()
    {
        var client = _factory.CreateClient();
        var projectId = await GatewayTestHelpers.CreateProjectAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            ProjectId = projectId,
            Title = "Run con artifact",
            Description = "Descripción",
            FlowType = Data.Models.DevFlowType.Discovery
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/execute-stage", new ExecuteStageRequest { InputText = "Necesito discovery" });

        var getResponse = await client.GetAsync($"/api/devflow/runs/{created.Id}");
        var run = await getResponse.Content.ReadFromJsonAsync<DevFlowRunDetailResponse>();

        Assert.NotNull(run);
        Assert.Single(run.Artifacts);
        Assert.False(string.IsNullOrWhiteSpace(run.Artifacts[0].PayloadJson));
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
/// Desde la tarea 10.2.1 (acceso por ownership) GET /api/devflow/runs/{id} ya no es
/// SuperUsuario-only: un usuario autenticado que no es dueño del proyecto del run
/// recibe 404 (no se revela la existencia de runs ajenos). Admin no es dueño del run.
/// </summary>
public class GetDevFlowRunAdminForbiddenTests : IClassFixture<GatewayApiFactoryAdmin>
{
    private readonly GatewayApiFactoryAdmin _factory;

    public GetDevFlowRunAdminForbiddenTests(GatewayApiFactoryAdmin factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDevFlowRun_ConAdmin_NoDueño_Devuelve404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devflow/runs/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
