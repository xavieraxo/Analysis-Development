using System.Net;
using System.Net.Http.Json;
using Data.Models;
using Gateway.Api.DTOs;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests de integración para POST /api/devflow/runs/{id}/approve.
/// </summary>
public class ApproveDevFlowGateTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;

    public ApproveDevFlowGateTests(GatewayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApproveGate_ConSuperUsuario_ApruebaGate_Devuelve200()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            Title = "Run para approve test",
            Description = "Desc"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/execute-stage", new ExecuteStageRequest { InputText = "x" });

        var approveRequest = new ApproveGateRequest
        {
            Stage = DevFlowStage.UR,
            Approved = true,
            Comment = "Aprobado para continuar"
        };
        var approveResponse = await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/approve", approveRequest);

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var result = await approveResponse.Content.ReadFromJsonAsync<ApproveGateResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Gate);
        Assert.Equal(DevFlowGateStatus.Approved, result.Gate.Status);
        Assert.Equal(1, result.Gate.DecidedByUserId);
        Assert.NotNull(result.Gate.DecidedAt);
        Assert.Equal("Aprobado para continuar", result.Gate.DecisionComment);
        Assert.Equal(DevFlowRunStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task ApproveGate_ConSuperUsuario_Rechaza_RunQuedaCancelled()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            Title = "Run para reject test",
            Description = "Desc"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/execute-stage", new ExecuteStageRequest { InputText = "x" });

        var rejectRequest = new ApproveGateRequest
        {
            Stage = DevFlowStage.UR,
            Approved = false,
            Comment = "No cumple criterios"
        };
        var approveResponse = await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/approve", rejectRequest);

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var result = await approveResponse.Content.ReadFromJsonAsync<ApproveGateResponse>();
        Assert.NotNull(result);
        Assert.Equal(DevFlowGateStatus.Rejected, result.Gate.Status);
        Assert.Equal(DevFlowRunStatus.Cancelled, result.Status);

        var getResponse = await client.GetAsync($"/api/devflow/runs/{created.Id}");
        var run = await getResponse.Content.ReadFromJsonAsync<DevFlowRunDetailResponse>();
        Assert.NotNull(run);
        Assert.Equal(DevFlowRunStatus.Cancelled, run.Status);
    }

    [Fact]
    public async Task ApproveGate_RunInexistente_Devuelve404()
    {
        var client = _factory.CreateClient();

        var request = new ApproveGateRequest { Stage = DevFlowStage.UR, Approved = true };
        var response = await client.PostAsJsonAsync("/api/devflow/runs/999999/approve", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApproveGate_Upsert_AprobarDosVeces_ActualizaGateSinDuplicados()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            Title = "Run para upsert test",
            Description = "Desc"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/execute-stage", new ExecuteStageRequest { InputText = "x" });

        var approve1 = await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/approve",
            new ApproveGateRequest { Stage = DevFlowStage.UR, Approved = true, Comment = "Primera aprobación" });
        Assert.Equal(HttpStatusCode.OK, approve1.StatusCode);
        var result1 = await approve1.Content.ReadFromJsonAsync<ApproveGateResponse>();
        Assert.NotNull(result1);
        var gateId = result1.Gate.Id;

        var approve2 = await client.PostAsJsonAsync($"/api/devflow/runs/{created.Id}/approve",
            new ApproveGateRequest { Stage = DevFlowStage.UR, Approved = true, Comment = "Segunda aprobación" });
        Assert.Equal(HttpStatusCode.OK, approve2.StatusCode);
        var result2 = await approve2.Content.ReadFromJsonAsync<ApproveGateResponse>();
        Assert.NotNull(result2);

        Assert.Equal(gateId, result2.Gate.Id);
        Assert.Equal("Segunda aprobación", result2.Gate.DecisionComment);

        var getResponse = await client.GetAsync($"/api/devflow/runs/{created.Id}");
        var run = await getResponse.Content.ReadFromJsonAsync<DevFlowRunDetailResponse>();
        Assert.NotNull(run);
        Assert.Single(run.Gates);
    }
}

/// <summary>
/// Verifica que Admin recibe 403 al aprobar gate.
/// </summary>
public class ApproveDevFlowGateAdminForbiddenTests : IClassFixture<GatewayApiFactoryAdmin>
{
    private readonly GatewayApiFactoryAdmin _factory;

    public ApproveDevFlowGateAdminForbiddenTests(GatewayApiFactoryAdmin factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApproveGate_ConAdmin_Devuelve403()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/devflow/runs/1/approve",
            new ApproveGateRequest { Stage = DevFlowStage.UR, Approved = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
