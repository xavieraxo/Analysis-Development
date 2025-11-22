using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared.Abstractions;

namespace MultiAgentSystem.Tests.Orchestration;

public class OrchestratorTests
{
    [Fact]
    public async Task RunAsync_EjecutaAgentesEnOrdenYEncadenaContexto()
    {
        // Arrange
        var pmAgent = new TestAgent(AgentRole.PM, "Plan inicial", "Plan final");
        var devAgent = new TestAgent(AgentRole.Dev, "Propuesta técnica");
        var fakeLlm = new FakeLlmClient();
        var orchestrator = new Orchestrator.App.Orchestrator(new IAgent[] { pmAgent, devAgent }, fakeLlm);

        var userMessage = new ChatMessage("conv-orch", AgentRole.User, "Necesito ayuda", DateTimeOffset.UtcNow);
        var flow = new[] { AgentRole.PM, AgentRole.Dev, AgentRole.PM };

        // Act
        var outputs = new List<ChatMessage>();
        await foreach (var message in orchestrator.RunAsync(userMessage.ConversationId, userMessage, flow))
        {
            outputs.Add(message);
        }

        // Assert
        Assert.Equal(3, outputs.Count);
        Assert.Equal(new[] { AgentRole.PM, AgentRole.Dev, AgentRole.PM }, outputs.Select(m => m.From).ToArray());

        Assert.Equal("Plan inicial", outputs[0].Text);
        Assert.Equal("Propuesta técnica", outputs[1].Text);
        Assert.Equal("Plan final", outputs[2].Text);

        Assert.Equal(2, pmAgent.Calls.Count);
        Assert.Equal(userMessage.Text, pmAgent.Calls[0].Context.Text);
        Assert.Single(devAgent.Calls);
        Assert.Equal("Plan inicial", devAgent.Calls.Single().Context.Text);
        Assert.Equal("Propuesta técnica", pmAgent.Calls[1].Context.Text);
    }
}
