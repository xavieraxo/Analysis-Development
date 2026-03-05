using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
        var response = await client.PostAsJsonAsync("/api/chat/run", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Body: {body}");
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();

        Assert.NotNull(chatResponse);
        Assert.Equal("respuesta-fake", chatResponse!.Summary.Text);
        Assert.Equal(new[] { AgentRole.UR, AgentRole.PM, AgentRole.PO, AgentRole.UX, AgentRole.Dev, AgentRole.PM },
            chatResponse.InternalConversations.Select(m => m.From).ToArray());
        Assert.Equal(new[] { "UR test", "Plan test", "PO test", "UX test", "Implementación test", "Cierre test" },
            chatResponse.InternalConversations.Select(m => m.Text).ToArray());
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
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.AddSingleton<IAgent>(new TestAgent(AgentRole.PM, "Plan test", "Cierre test"));
            services.AddSingleton<IAgent>(new TestAgent(AgentRole.Dev, "Implementación test"));
            services.AddSingleton<IAgent>(new TestAgent(AgentRole.UR, "UR test"));
            services.AddSingleton<IAgent>(new TestAgent(AgentRole.PO, "PO test"));
            services.AddSingleton<IAgent>(new TestAgent(AgentRole.UX, "UX test"));
            services.AddSingleton<ILlmClient>(new FakeLlmClient());
        });
    }
}
