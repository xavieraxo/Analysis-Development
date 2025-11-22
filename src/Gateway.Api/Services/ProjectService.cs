using Data.Models;
using Gateway.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Services;

public interface IProjectService
{
    Task<ProjectDto> CreateProjectAsync(int userId, string name, string description);
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

