using System.Net;
using System.Net.Http.Json;
using Data.Models;
using Gateway.Api.DTOs;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests de integración para POST /api/devflow/runs/{id}/execute-stage.
/// </summary>
public class ExecuteDevFlowStageTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;

    public ExecuteDevFlowStageTests(GatewayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExecuteStage_ConSuperUsuario_EjecutaUR_Devuelve200()
    {
        var client = _factory.CreateClient();

        var createRequest = new CreateDevFlowRunRequest
        {
            Title = "Run para execute-stage",
            Description = "Idea de cambio"
        };
        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        var executeRequest = new ExecuteStageRequest { InputText = "Necesito un sistema de tickets" };
        var executeResponse = await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/execute-stage", executeRequest);

        Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);
        var result = await executeResponse.Content.ReadFromJsonAsync<ExecuteStageResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Run);
        Assert.NotNull(result.Artifact);

        Assert.Equal(created.Id, result.Run.Id);
        Assert.Equal(DevFlowStage.PM, result.Run.CurrentStage);
        Assert.Equal(DevFlowRunStatus.InProgress, result.Run.Status);
        Assert.Single(result.Run.Artifacts);
        Assert.Single(result.Run.Gates);

        Assert.True(result.Artifact.Id > 0);
        Assert.Equal(DevFlowStage.UR, result.Artifact.Stage);
        Assert.Equal(1, result.Artifact.Version);
    }

    [Fact]
    public async Task ExecuteStage_SeCreaDevFlowArtifactYRunAvanza()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            Title = "Test artifact",
            Description = "Desc"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/execute-stage", new ExecuteStageRequest { InputText = "x" });
        var getResponse = await client.GetAsync($"/api/devflow/runs/{created.Id}");
        var run = await getResponse.Content.ReadFromJsonAsync<DevFlowRunDetailResponse>();
        Assert.NotNull(run);
        Assert.Equal(DevFlowStage.PM, run.CurrentStage);
        Assert.Single(run.Artifacts);
        Assert.Equal(DevFlowStage.UR, run.Artifacts[0].Stage);
    }

    [Fact]
    public async Task ExecuteStage_RunInexistente_Devuelve404()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/devflow/runs/999999/execute-stage", new ExecuteStageRequest { InputText = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExecuteStage_GatePending_Devuelve409()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            Title = "Run para gate test",
            Description = "Desc"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/execute-stage", new ExecuteStageRequest { InputText = "ur" });
        var executePmResponse = await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/execute-stage",
            new ExecuteStageRequest { OverrideStage = DevFlowStage.PM, InputText = "pm" });

        Assert.Equal(HttpStatusCode.Conflict, executePmResponse.StatusCode);
    }
}

/// <summary>
/// Verifica que Admin recibe 403 al ejecutar etapa.
/// </summary>
public class ExecuteDevFlowStageAdminForbiddenTests : IClassFixture<GatewayApiFactoryAdmin>
{
    private readonly GatewayApiFactoryAdmin _factory;

    public ExecuteDevFlowStageAdminForbiddenTests(GatewayApiFactoryAdmin factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExecuteStage_ConAdmin_Devuelve403()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/devflow/runs/1/execute-stage", new ExecuteStageRequest { InputText = "test" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
