using System.Net;
using System.Net.Http.Json;
using Data;
using Data.Models;
using Gateway.Api.DTOs;
using Gateway.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests unitarios para BranchPlanMarkdownFormatter.
/// </summary>
public class BranchPlanMarkdownFormatterTests
{
    [Fact]
    public void ToMarkdown_GeneraTablaYDescripciones()
    {
        var dto = new BranchPlanExportDto
        {
            RunId = 1,
            BranchPlanId = 10,
            Items =
            [
                new BranchPlanItemExportDto { Order = 1, StoryId = "5.2", TaskId = "5.2.1", Area = "rag", BranchName = "feature/rag/5.2.1-ctx", Title = "Context injection", Description = "Inyectar RAG" },
                new BranchPlanItemExportDto { Order = 2, StoryId = "5.2", TaskId = "5.2.2", Area = "rag", BranchName = "feature/rag/5.2.2-retriever", Title = "IRetriever", Description = null }
            ]
        };

        var md = BranchPlanMarkdownFormatter.ToMarkdown(dto, 1);

        Assert.Contains("# Branch Plan – DevFlow Run 1", md);
        Assert.Contains("| Order | Story | Task | Branch | Title |", md);
        Assert.Contains("feature/rag/5.2.1-ctx", md);
        Assert.Contains("## Descripciones", md);
        Assert.Contains("### 5.2.1 - Context injection", md);
        Assert.Contains("Inyectar RAG", md);
    }
}

/// <summary>
/// Tests de integración para GET /api/devflow/runs/{id}/branch-plan.
/// </summary>
public class BranchPlanExportTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;

    public BranchPlanExportTests(GatewayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBranchPlan_ConSuperUsuario_ExistePlan_Devuelve200Json()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            Title = "Run con BranchPlan",
            Description = "Test export"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        await SeedBranchPlanAsync(created.Id);

        var response = await client.GetAsync($"/api/devflow/runs/{created.Id}/branch-plan");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var plan = await response.Content.ReadFromJsonAsync<BranchPlanExportDto>();
        Assert.NotNull(plan);
        Assert.Equal(created.Id, plan.RunId);
        Assert.True(plan.BranchPlanId > 0);
        Assert.Equal(2, plan.Items.Count);
        Assert.Equal("feature/rag/5.2.1-context-injection", plan.Items[0].BranchName);
    }

    [Fact]
    public async Task GetBranchPlan_FormatMd_Devuelve200Markdown()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            Title = "Run para MD test",
            Description = "Test"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        await SeedBranchPlanAsync(created.Id);

        var response = await client.GetAsync($"/api/devflow/runs/{created.Id}/branch-plan?format=md");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/markdown", response.Content.Headers.ContentType?.MediaType);
        var markdown = await response.Content.ReadAsStringAsync();
        Assert.Contains("# Branch Plan", markdown);
        Assert.Contains("| Order | Story | Task | Branch | Title |", markdown);
        Assert.Contains("feature/rag/5.2.1-context-injection", markdown);
    }

    [Fact]
    public async Task GetBranchPlan_RunInexistente_Devuelve404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devflow/runs/999999/branch-plan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBranchPlan_RunSinBranchPlan_Devuelve404()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/devflow/runs", new CreateDevFlowRunRequest
        {
            Title = "Run sin plan",
            Description = "Sin BranchPlan"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DevFlowRunResponse>();
        Assert.NotNull(created);

        var response = await client.GetAsync($"/api/devflow/runs/{created.Id}/branch-plan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SeedBranchPlanAsync(int runId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var plan = new BranchPlan
        {
            DevFlowRunId = runId,
            CreatedAt = DateTime.UtcNow,
            FormatVersion = 1
        };
        plan.Items.Add(new BranchPlanItem
        {
            StoryId = "5.2",
            TaskId = "5.2.1",
            Area = "rag",
            BranchName = "feature/rag/5.2.1-context-injection",
            Title = "Context injection en prompts",
            Description = "Inyectar contexto RAG",
            Order = 1,
            CreatedAt = DateTime.UtcNow
        });
        plan.Items.Add(new BranchPlanItem
        {
            StoryId = "5.2",
            TaskId = "5.2.2",
            Area = "rag",
            BranchName = "feature/rag/5.2.2-retriever-integration",
            Title = "Integrar IRetriever",
            Order = 2,
            CreatedAt = DateTime.UtcNow
        });

        db.BranchPlans.Add(plan);
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Verifica que Admin recibe 403 al obtener branch-plan.
/// </summary>
public class BranchPlanExportAdminForbiddenTests : IClassFixture<GatewayApiFactoryAdmin>
{
    private readonly GatewayApiFactoryAdmin _factory;

    public BranchPlanExportAdminForbiddenTests(GatewayApiFactoryAdmin factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBranchPlan_ConAdmin_Devuelve403()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devflow/runs/1/branch-plan");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
