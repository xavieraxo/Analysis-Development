using Data;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests para persistencia de BranchPlan y BranchPlanItem.
/// </summary>
public class BranchPlanTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public BranchPlanTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task BranchPlan_CanBePersistedWithItems()
    {
        var user = new ApplicationUser
        {
            UserName = "branch@devflow.local",
            Email = "branch@devflow.local",
            Name = "Branch User",
            Role = UserRole.SuperUsuario,
            IsActive = true,
            EmailConfirmed = true
        };
        var hasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, "Test123!");
        _context.ApplicationUsers.Add(user);

        var run = new DevFlowRun
        {
            Title = "RAG MVP",
            Description = "Implementar RAG",
            Status = DevFlowRunStatus.Completed,
            CurrentStage = DevFlowStage.DEV,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync();

        var plan = new BranchPlan
        {
            DevFlowRunId = run.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            FormatVersion = 1
        };
        plan.Items.Add(new BranchPlanItem
        {
            StoryId = "5.2",
            TaskId = "5.2.1",
            Area = "rag",
            BranchName = "feature/rag/context-injection",
            Title = "Context injection en prompts",
            Description = "Inyectar contexto RAG en los prompts de agentes",
            Order = 1,
            CreatedAt = DateTime.UtcNow
        });
        plan.Items.Add(new BranchPlanItem
        {
            StoryId = "5.2",
            TaskId = "5.2.2",
            Area = "rag",
            BranchName = "feature/rag/retriever-integration",
            Title = "Integrar IRetriever",
            Description = null,
            Order = 2,
            CreatedAt = DateTime.UtcNow
        });

        _context.BranchPlans.Add(plan);
        await _context.SaveChangesAsync();

        var retrieved = await _context.BranchPlans
            .Include(p => p.Items.OrderBy(i => i.Order))
            .Include(p => p.DevFlowRun)
            .FirstOrDefaultAsync(p => p.Id == plan.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(run.Id, retrieved.DevFlowRunId);
        Assert.Equal(2, retrieved.Items.Count);
        Assert.Equal("feature/rag/context-injection", retrieved.Items[0].BranchName);
        Assert.Equal("feature/rag/retriever-integration", retrieved.Items[1].BranchName);
        Assert.NotNull(retrieved.DevFlowRun);
        Assert.Equal("RAG MVP", retrieved.DevFlowRun.Title);
    }

    [Fact]
    public async Task BranchPlan_RelationWithDevFlowRun_FKWorks()
    {
        var user = new ApplicationUser
        {
            UserName = "fk@devflow.local",
            Email = "fk@devflow.local",
            Name = "FK User",
            Role = UserRole.SuperUsuario,
            IsActive = true,
            EmailConfirmed = true
        };
        var hasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, "Test123!");
        _context.ApplicationUsers.Add(user);

        var run = new DevFlowRun
        {
            Title = "Run for BranchPlan FK",
            Description = "Test FK",
            Status = DevFlowRunStatus.InProgress,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync();

        var plan = new BranchPlan
        {
            DevFlowRunId = run.Id,
            CreatedAt = DateTime.UtcNow,
            FormatVersion = 1
        };
        plan.Items.Add(new BranchPlanItem
        {
            StoryId = "1",
            TaskId = "1.1",
            Area = "autodev",
            BranchName = "feature/autodev/test",
            Title = "Test",
            Order = 1,
            CreatedAt = DateTime.UtcNow
        });

        _context.BranchPlans.Add(plan);
        await _context.SaveChangesAsync();

        var runWithPlan = await _context.DevFlowRuns
            .Include(r => r.BranchPlan!)
            .ThenInclude(bp => bp!.Items)
            .FirstAsync(r => r.Id == run.Id);

        Assert.NotNull(runWithPlan.BranchPlan);
        Assert.Equal(plan.Id, runWithPlan.BranchPlan.Id);
        Assert.Single(runWithPlan.BranchPlan.Items);
        Assert.Equal("feature/autodev/test", runWithPlan.BranchPlan.Items[0].BranchName);
    }

    public void Dispose() => _context.Dispose();
}
