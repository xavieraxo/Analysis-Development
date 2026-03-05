using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Data.Models;
using Gateway.Api.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests de integración para POST /api/devflow/runs.
/// </summary>
public class CreateDevFlowRunTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;

    public CreateDevFlowRunTests(GatewayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateDevFlowRun_ConSuperUsuario_Devuelve201()
    {
        var client = _factory.CreateClient();
        var request = new CreateDevFlowRunRequest
        {
            Title = "Implementar login con OAuth",
            Description = "Integrar autenticación con proveedores externos"
        };

        var response = await client.PostAsJsonAsync("/api/devflow/runs", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var run = await response.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(run);
        Assert.True(run.Id > 0);
        Assert.Equal("Implementar login con OAuth", run.Title);
        Assert.Equal("Integrar autenticación con proveedores externos", run.Description);
        Assert.Equal(DevFlowRunStatus.Created, run.Status);
        Assert.Equal(DevFlowStage.UR, run.CurrentStage);
    }

    [Fact]
    public async Task CreateDevFlowRun_SinTitulo_Devuelve400()
    {
        var client = _factory.CreateClient();
        var request = new CreateDevFlowRunRequest
        {
            Title = "",
            Description = "Descripción sin título"
        };

        var response = await client.PostAsJsonAsync("/api/devflow/runs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

/// <summary>
/// Verifica que Admin recibe 403 al crear DevFlow run.
/// </summary>
public class CreateDevFlowRunAdminForbiddenTests : IClassFixture<GatewayApiFactoryAdmin>
{
    private readonly GatewayApiFactoryAdmin _factory;

    public CreateDevFlowRunAdminForbiddenTests(GatewayApiFactoryAdmin factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateDevFlowRun_ConAdmin_Devuelve403()
    {
        var client = _factory.CreateClient();
        var request = new CreateDevFlowRunRequest
        {
            Title = "Test run",
            Description = "Test"
        };

        var response = await client.PostAsJsonAsync("/api/devflow/runs", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
