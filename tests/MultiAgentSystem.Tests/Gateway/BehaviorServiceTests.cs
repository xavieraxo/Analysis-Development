using Data;
using Gateway.Api.DTOs;
using Gateway.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Abstractions;

namespace MultiAgentSystem.Tests.Gateway;

public class BehaviorServiceTests
{
    private static IDbContextFactory<ApplicationDbContext> CreateDbFactory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task UpsertAndGet_ShouldPersistBehavior()
    {
        var factory = CreateDbFactory();
        var service = new BehaviorService(factory, NullLogger<BehaviorService>.Instance);

        var request = new BehaviorUpsertRequest
        {
            AgentRole = AgentRole.Dev,
            Alias = "Dev Alias",
            Prompt = new string('a', 1500),
            Instructions = new List<BehaviorInstruction>
            {
                new() { Question = "Q1", Answer = "A1" }
            }
        };

        var saved = await service.UpsertAsync(request);
        Assert.False(saved.FromFallback);
        Assert.Equal("Dev Alias", saved.Alias);
        Assert.Single(saved.Instructions);

        var fetched = await service.GetByRoleAsync(AgentRole.Dev);
        Assert.False(fetched.FromFallback);
        Assert.Equal("Dev Alias", fetched.Alias);
        Assert.Single(fetched.Instructions);
    }

    [Fact]
    public async Task GetByRole_WhenMissing_ReturnsFallback()
    {
        var factory = CreateDbFactory();
        var service = new BehaviorService(factory, NullLogger<BehaviorService>.Instance);

        var behavior = await service.GetByRoleAsync(AgentRole.PM);

        Assert.True(behavior.FromFallback);
        Assert.False(string.IsNullOrWhiteSpace(behavior.Prompt));
    }

    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(_options);
        }

        public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<ApplicationDbContext>(CreateDbContext());
        }
    }
}

