using Data.Models;
using Gateway.Api.DTOs;
using Gateway.Api.Services;
using Shared.Abstractions;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests unitarios para DevFlowAgentDispatcher.
/// </summary>
public class DevFlowAgentDispatcherTests
{
    private static IDevFlowAgentDispatcher CreateDispatcher(params IAgent[] agents)
    {
        var pipeline = new DevFlowPipeline();
        return new DevFlowAgentDispatcher(pipeline, agents);
    }

    [Theory]
    [InlineData(DevFlowStage.UR, AgentRole.UR)]
    [InlineData(DevFlowStage.PM, AgentRole.PM)]
    [InlineData(DevFlowStage.PO, AgentRole.PO)]
    [InlineData(DevFlowStage.DEV, AgentRole.Dev)]
    public void ResolveAgent_StageTieneAgente_DevuelveAgenteCorrecto(DevFlowStage stage, AgentRole expectedRole)
    {
        var agents = new IAgent[]
        {
            new TestAgent(AgentRole.UR, "out-ur"),
            new TestAgent(AgentRole.PM, "out-pm"),
            new TestAgent(AgentRole.PO, "out-po"),
            new TestAgent(AgentRole.Dev, "out-dev"),
        };
        var dispatcher = CreateDispatcher(agents);

        var agent = dispatcher.ResolveAgent(stage);

        Assert.Equal(expectedRole, agent.Role);
    }

    [Fact]
    public void ResolveAgent_StageSinAgenteRegistrado_LanzaInvalidOperationException()
    {
        var agents = new IAgent[]
        {
            new TestAgent(AgentRole.UR),
            new TestAgent(AgentRole.PM),
            new TestAgent(AgentRole.PO),
            // Sin Dev
        };
        var dispatcher = CreateDispatcher(agents);

        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.ResolveAgent(DevFlowStage.DEV));

        Assert.Contains("DEV", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_StageUR_DevuelveDevFlowAgentResultConPayloadDelAgente()
    {
        var urAgent = new TestAgent(AgentRole.UR, "respuesta-ur-payload");
        var dispatcher = CreateDispatcher(urAgent, new TestAgent(AgentRole.PM), new TestAgent(AgentRole.PO), new TestAgent(AgentRole.Dev));

        var input = new DevFlowAgentInput
        {
            RunId = 42,
            ConversationId = "conv-1",
            UserMessage = "Necesito un sistema de tickets"
        };

        var result = await dispatcher.ExecuteAsync(DevFlowStage.UR, input);

        Assert.Equal(AgentRole.UR, result.AgentRole);
        Assert.Equal(DevFlowStage.UR, result.Stage);
        Assert.Equal("respuesta-ur-payload", result.PayloadJson);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task ExecuteAsync_ConPreviousArtifacts_IncluyeEnContextoDelAgente()
    {
        var pmAgent = new TestAgent(AgentRole.PM, "respuesta-pm");
        var dispatcher = CreateDispatcher(new TestAgent(AgentRole.UR), pmAgent, new TestAgent(AgentRole.PO), new TestAgent(AgentRole.Dev));

        var input = new DevFlowAgentInput
        {
            RunId = 1,
            UserMessage = "idea",
            PreviousArtifactsSummary = "UR: resumen previo"
        };

        await dispatcher.ExecuteAsync(DevFlowStage.PM, input);

        Assert.Single(pmAgent.Calls);
        Assert.Contains("Resumen de artefactos previos", pmAgent.Calls[0].Context.Text);
        Assert.Contains("UR: resumen previo", pmAgent.Calls[0].Context.Text);
    }

    [Fact]
    public async Task ExecuteAsync_SinConversationId_UsaDevflowRunId()
    {
        var urAgent = new TestAgent(AgentRole.UR, "ok");
        var dispatcher = CreateDispatcher(urAgent, new TestAgent(AgentRole.PM), new TestAgent(AgentRole.PO), new TestAgent(AgentRole.Dev));

        var input = new DevFlowAgentInput { RunId = 99, UserMessage = "msg" };

        await dispatcher.ExecuteAsync(DevFlowStage.UR, input);

        Assert.Equal("devflow-99", urAgent.Calls[0].ConversationId);
    }

    [Fact]
    public async Task ExecuteAsync_StageSinAgente_LanzaInvalidOperationException()
    {
        var agents = new IAgent[] { new TestAgent(AgentRole.UR), new TestAgent(AgentRole.PM), new TestAgent(AgentRole.PO) };
        var dispatcher = CreateDispatcher(agents);

        var input = new DevFlowAgentInput { RunId = 1, UserMessage = "x" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.ExecuteAsync(DevFlowStage.DEV, input));
    }
}
