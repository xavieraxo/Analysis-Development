using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agents.Dev;
using Shared.Abstractions;

namespace MultiAgentSystem.Tests.Agents;

public class DevAgentTests
{
    [Fact]
    public async Task HandleAsync_InvocaLlmYDevuelveRespuestaDelDev()
    {
        // Arrange
        var llm = new FakeLlmClient("Diseño técnico");
        var agent = new DevAgent(llm);
        var context = new ChatMessage("conv-dev", AgentRole.User, "Necesito un API", DateTimeOffset.UtcNow);
        var turn = new AgentTurn("conv-dev", AgentRole.Dev, context, CancellationToken.None);

        // Act
        var before = DateTimeOffset.UtcNow;
        var result = await agent.HandleAsync(turn);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal("conv-dev", result.ConversationId);
        Assert.Equal(AgentRole.Dev, result.From);
        Assert.Equal("Diseño técnico", result.Text);
        Assert.InRange(result.At, before, after);

        Assert.Single(llm.Calls);
        var call = llm.Calls.First();
        Assert.Contains("Requisitos:", call.user);
        Assert.Contains("Entrega:", call.user);
        Assert.Contains("desarrollador .NET", call.system, StringComparison.OrdinalIgnoreCase);
    }
}
