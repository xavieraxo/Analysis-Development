using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Data.Models;
using Gateway.Api.DTOs;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests de integración para GET /api/devflow/runs (listar con filtros).
/// </summary>
public class ListDevFlowRunsTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;

    public ListDevFlowRunsTests(GatewayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListDevFlowRuns_ConSuperUsuario_SinRuns_Devuelve200ConListaVacia()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devflow/runs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<DevFlowRunListItem>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task ListDevFlowRuns_Paginacion_CreaRunsYVerificaTotal()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest { Title = "Run 1", Description = "D1" });
        await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest { Title = "Run 2", Description = "D2" });

        var response = await client.GetAsync("/api/devflow/runs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<DevFlowRunListItem>>();
        Assert.NotNull(result);
        Assert.True(result.Total >= 2);
        Assert.True(result.Items.Count >= 2);
    }

    [Fact]
    public async Task ListDevFlowRuns_FiltroPorStatus_DevuelveFiltrado()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devflow/runs?status=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<DevFlowRunListItem>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        foreach (var item in result.Items)
            Assert.Equal(DevFlowRunStatus.Created, item.Status);
    }

    [Fact]
    public async Task ListDevFlowRuns_PageSizeInvalido_Devuelve400()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devflow/runs?pageSize=150");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

/// <summary>
/// Verifica que Admin recibe 403 al listar DevFlow runs.
/// </summary>
public class ListDevFlowRunsAdminForbiddenTests : IClassFixture<GatewayApiFactoryAdmin>
{
    private readonly GatewayApiFactoryAdmin _factory;

    public ListDevFlowRunsAdminForbiddenTests(GatewayApiFactoryAdmin factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListDevFlowRuns_ConAdmin_Devuelve403()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devflow/runs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
