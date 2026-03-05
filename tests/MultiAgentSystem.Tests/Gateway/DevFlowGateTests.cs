using Data;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MultiAgentSystem.Tests.Gateway;

public class DevFlowGateTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public DevFlowGateTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task DevFlowGate_CanBeCreatedAndRetrieved()
    {
        var user = new ApplicationUser
        {
            UserName = "gate@devflow.local",
            Email = "gate@devflow.local",
            Name = "Gate User",
            Role = UserRole.SuperUsuario,
            IsActive = true,
            EmailConfirmed = true
        };
        var hasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, "Test123!");
        _context.ApplicationUsers.Add(user);

        var run = new DevFlowRun
        {
            Title = "Feature X",
            Description = "Implement X",
            Status = DevFlowRunStatus.Created,
            CurrentStage = null,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync();

        var gate = new DevFlowGate
        {
            DevFlowRunId = run.Id,
            Stage = DevFlowStage.UR,
            Status = DevFlowGateStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.DevFlowGates.Add(gate);
        await _context.SaveChangesAsync();

        var retrieved = await _context.DevFlowGates
            .Include(g => g.DevFlowRun)
            .FirstOrDefaultAsync(g => g.Id == gate.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(DevFlowStage.UR, retrieved.Stage);
        Assert.Equal(DevFlowGateStatus.Pending, retrieved.Status);
        Assert.Equal(run.Id, retrieved.DevFlowRunId);
        Assert.NotNull(retrieved.DevFlowRun);
        Assert.Equal("Feature X", retrieved.DevFlowRun.Title);
    }

    [Fact]
    public async Task DevFlowGate_Approved_CanBePersistedWithDecidedByUser()
    {
        var user = new ApplicationUser
        {
            UserName = "approver@devflow.local",
            Email = "approver@devflow.local",
            Name = "Approver",
            Role = UserRole.SuperUsuario,
            IsActive = true,
            EmailConfirmed = true
        };
        var hasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, "Test123!");
        _context.ApplicationUsers.Add(user);

        var run = new DevFlowRun
        {
            Title = "Run for approval",
            Description = "Test",
            Status = DevFlowRunStatus.InProgress,
            CurrentStage = DevFlowStage.PM,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync();

        var gate = new DevFlowGate
        {
            DevFlowRunId = run.Id,
            Stage = DevFlowStage.UR,
            Status = DevFlowGateStatus.Approved,
            DecisionComment = "Looks good",
            DecidedByUserId = user.Id,
            DecidedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.DevFlowGates.Add(gate);
        await _context.SaveChangesAsync();

        var retrieved = await _context.DevFlowGates
            .Include(g => g.DecidedByUser)
            .FirstAsync(g => g.Id == gate.Id);

        Assert.Equal(DevFlowGateStatus.Approved, retrieved.Status);
        Assert.Equal("Looks good", retrieved.DecisionComment);
        Assert.NotNull(retrieved.DecidedByUser);
        Assert.Equal("Approver", retrieved.DecidedByUser.Name);
    }

    [Fact]
    public async Task DevFlowRun_WithGates_NavigationWorks()
    {
        var user = new ApplicationUser
        {
            UserName = "nav@devflow.local",
            Email = "nav@devflow.local",
            Name = "Nav User",
            Role = UserRole.Final,
            IsActive = true,
            EmailConfirmed = true
        };
        var hasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, "Test123!");
        _context.ApplicationUsers.Add(user);

        var run = new DevFlowRun
        {
            Title = "Run with gates",
            Description = "Test",
            Status = DevFlowRunStatus.Created,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync();

        var gate1 = new DevFlowGate { DevFlowRunId = run.Id, Stage = DevFlowStage.UR, Status = DevFlowGateStatus.Pending, CreatedAt = DateTime.UtcNow };
        var gate2 = new DevFlowGate { DevFlowRunId = run.Id, Stage = DevFlowStage.PM, Status = DevFlowGateStatus.Pending, CreatedAt = DateTime.UtcNow };

        _context.DevFlowGates.AddRange(gate1, gate2);
        await _context.SaveChangesAsync();

        var retrieved = await _context.DevFlowRuns
            .Include(r => r.Gates)
            .FirstAsync(r => r.Id == run.Id);

        Assert.Equal(2, retrieved.Gates.Count);
        Assert.Contains(retrieved.Gates, g => g.Stage == DevFlowStage.UR);
        Assert.Contains(retrieved.Gates, g => g.Stage == DevFlowStage.PM);
    }

    public void Dispose() => _context.Dispose();
}
