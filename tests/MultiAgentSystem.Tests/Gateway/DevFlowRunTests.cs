using Data;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MultiAgentSystem.Tests.Gateway;

public class DevFlowRunTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public DevFlowRunTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task DevFlowRun_CanBeCreatedAndRetrieved()
    {
        var user = new ApplicationUser
        {
            UserName = "test@devflow.local",
            Email = "test@devflow.local",
            Name = "Test User",
            Role = UserRole.Final,
            IsActive = true,
            EmailConfirmed = true
        };
        var hasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, "Test123!");
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var run = new DevFlowRun
        {
            Title = "Add login feature",
            Description = "Implement user authentication with JWT",
            Status = DevFlowRunStatus.Created,
            CurrentStage = null,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync();

        var retrieved = await _context.DevFlowRuns
            .Include(r => r.CreatedByUser)
            .FirstOrDefaultAsync(r => r.Id == run.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("Add login feature", retrieved.Title);
        Assert.Equal(DevFlowRunStatus.Created, retrieved.Status);
        Assert.Equal(user.Id, retrieved.CreatedByUserId);
        Assert.Equal("Test User", retrieved.CreatedByUser.Name);
    }

    [Fact]
    public async Task DevFlowRun_WithProjectId_CanBePersisted()
    {
        var user = new ApplicationUser
        {
            UserName = "test2@devflow.local",
            Email = "test2@devflow.local",
            Name = "Test User 2",
            Role = UserRole.Final,
            IsActive = true,
            EmailConfirmed = true
        };
        var hasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, "Test123!");
        _context.ApplicationUsers.Add(user);

        var legacyUser = new User
        {
            Email = "legacy@test.local",
            Name = "Legacy",
            Role = UserRole.Final,
            PasswordHash = "hash",
            IsActive = true
        };
        _context.Users.Add(legacyUser);
        await _context.SaveChangesAsync();

        var project = new Project
        {
            Name = "MVP Project",
            Description = "Test project",
            UserId = legacyUser.Id,
            Status = ProjectStatus.InProgress
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var run = new DevFlowRun
        {
            Title = "Refactor API",
            Description = "Improve endpoint structure",
            ProjectId = project.Id,
            Status = DevFlowRunStatus.InProgress,
            CurrentStage = DevFlowStage.PM,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync();

        var retrieved = await _context.DevFlowRuns
            .Include(r => r.Project)
            .FirstAsync(r => r.Id == run.Id);

        Assert.NotNull(retrieved.Project);
        Assert.Equal(project.Id, retrieved.ProjectId);
        Assert.Equal(DevFlowStage.PM, retrieved.CurrentStage);
    }

    public void Dispose() => _context.Dispose();
}
