using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Abstractions;

namespace MultiAgentSystem.Tests.Gateway;

public class GatewayApiTests : IClassFixture<GatewayApiFactory>
{
    private readonly GatewayApiFactory _factory;

    public GatewayApiTests(GatewayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostChatRun_DevuelveMensajesDeLosAgentes()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new ChatMessage("conv-gateway", AgentRole.User, "Necesito un MVP", DateTimeOffset.UtcNow);

        // Act
        var response = await client.PostAsJsonAsync("/chat/run", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessage>>();

        Assert.NotNull(messages);
        Assert.Equal(3, messages.Count);
        Assert.Equal(new[] { AgentRole.PM, AgentRole.Dev, AgentRole.PM }, messages.Select(m => m.From).ToArray());
        Assert.Equal("Plan test", messages![0].Text);
        Assert.Equal("Implementación test", messages[1].Text);
        Assert.Equal("Cierre test", messages[2].Text);
    }
}

public class GatewayApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAgent>();
            services.RemoveAll<ILlmClient>();

            services.AddSingleton<IAgent>(new TestAgent(AgentRole.PM, "Plan test", "Cierre test"));
            services.AddSingleton<IAgent>(new TestAgent(AgentRole.Dev, "Implementación test"));
            services.AddSingleton<ILlmClient>(new FakeLlmClient());
        });
    }
}
