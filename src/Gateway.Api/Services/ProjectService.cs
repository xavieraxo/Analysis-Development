using Data.Models;
using Gateway.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Services;

public interface IProjectService
{
    Task<ProjectDto> CreateProjectAsync(int userId, string name, string description);
    Task<ProjectWithDevFlowDto> CreateProjectWithInitialDevFlowRunAsync(CreateProjectRequest request, int currentUserId, CancellationToken cancellationToken = default);
    Task<List<ProjectDto>> GetUserProjectsAsync(int userId);
    Task<ProjectDto?> GetProjectByIdAsync(int projectId, int userId);
    Task<List<ProjectDto>> GetAllProjectsAsync(); // Solo Admin/SuperUsuario
    Task<bool> UpdateProjectStatusAsync(int projectId, ProjectStatus status);
    Task SaveProjectAnalysisAsync(int projectId, string conversationId, string summary, string internalConversationsJson);
}

public class ProjectService : IProjectService
{
    private readonly Data.ApplicationDbContext _context;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(Data.ApplicationDbContext context, ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProjectDto> CreateProjectAsync(int userId, string name, string description)
    {
        var project = new Project
        {
            Name = name,
            Description = description,
            UserId = userId,
            Status = ProjectStatus.InProgress
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return MapToDto(project);
    }

    public async Task<ProjectWithDevFlowDto> CreateProjectWithInitialDevFlowRunAsync(
        CreateProjectRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Regla de licencia individual: un solo proyecto activo (InProgress u OnHold) por usuario.
        var hasActiveProject = await _context.Projects
            .AnyAsync(p =>
                p.UserId == currentUserId &&
                (p.Status == ProjectStatus.InProgress || p.Status == ProjectStatus.OnHold),
                cancellationToken);

        if (hasActiveProject)
        {
            throw new InvalidOperationException("El usuario ya tiene un proyecto activo.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            UserId = currentUserId,
            Status = ProjectStatus.InProgress
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        var run = new DevFlowRun
        {
            ProjectId = project.Id,
            Title = request.Name,
            Description = request.Description,
            Status = DevFlowRunStatus.Created,
            CurrentStage = DevFlowStage.UR,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Scope = DevFlowScope.UserProject,
            IsMigrated = false
        };

        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        var projectDto = MapToDto(project);
        var runDto = new DevFlowRunResponse
        {
            Id = run.Id,
            ProjectId = run.ProjectId,
            Title = run.Title,
            Description = run.Description,
            Status = run.Status,
            CurrentStage = run.CurrentStage,
            CreatedAt = run.CreatedAt,
            UpdatedAt = run.UpdatedAt
        };

        return new ProjectWithDevFlowDto(projectDto, runDto);
    }

    public async Task<List<ProjectDto>> GetUserProjectsAsync(int userId)
    {
        var projects = await _context.Projects
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToDto).ToList();
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(int projectId, int userId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

        return project == null ? null : MapToDto(project);
    }

    public async Task<List<ProjectDto>> GetAllProjectsAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToDto).ToList();
    }

    public async Task<bool> UpdateProjectStatusAsync(int projectId, ProjectStatus status)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return false;

        project.Status = status;
        if (status == ProjectStatus.Completed)
        {
            project.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task SaveProjectAnalysisAsync(int projectId, string conversationId, string summary, string internalConversationsJson)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return;

        project.ConversationId = conversationId;
        project.Summary = summary;
        project.InternalConversationsJson = internalConversationsJson;

        await _context.SaveChangesAsync();
    }

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            project.Status,
            project.UserId,
            project.User?.Name ?? "Unknown",
            project.CreatedAt,
            project.CompletedAt,
            project.ConversationId,
            project.Summary);
    }
}

public record ProjectDto(
    int Id,
    string Name,
    string Description,
    ProjectStatus Status,
    int UserId,
    string UserName,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? ConversationId,
    string? Summary);

public record ProjectWithDevFlowDto(
    ProjectDto Project,
    DevFlowRunResponse InitialRun);


