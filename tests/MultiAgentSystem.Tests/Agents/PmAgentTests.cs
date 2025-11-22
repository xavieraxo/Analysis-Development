using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agents.PM;
using Shared.Abstractions;

namespace MultiAgentSystem.Tests.Agents;

public class PmAgentTests
{
    [Fact]
    public async Task HandleAsync_InvocaLlmYDevuelveMensajeDelPm()
    {
        // Arrange
        var llm = new FakeLlmClient("Plan de prueba");
        var agent = new PmAgent(llm);
        var context = new ChatMessage("conv-1", AgentRole.User, "Necesito un plan", DateTimeOffset.UtcNow);
        var turn = new AgentTurn("conv-1", AgentRole.PM, context, CancellationToken.None);

        // Act
        var before = DateTimeOffset.UtcNow;
        var result = await agent.HandleAsync(turn);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal("conv-1", result.ConversationId);
        Assert.Equal(AgentRole.PM, result.From);
        Assert.Equal("Plan de prueba", result.Text);
        Assert.InRange(result.At, before, after);

        Assert.Single(llm.Calls);
        var call = llm.Calls.First();
        Assert.Contains("Contexto:", call.user);
        Assert.Contains("Tarea:", call.user);
        Assert.Contains("Project Manager", call.system);
    }
}
