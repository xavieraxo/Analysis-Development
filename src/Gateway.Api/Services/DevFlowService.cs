using Data;
using Data.Models;
using Gateway.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Services;

public class DevFlowService : IDevFlowService
{
    private readonly ApplicationDbContext _context;

    public DevFlowService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DevFlowRunResponse?> CreateRunAsync(CreateDevFlowRunRequest request, int createdByUserId)
    {
        if (request.ProjectId.HasValue)
        {
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId.Value);
            if (!projectExists)
                return null;
        }

        var run = new DevFlowRun
        {
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Status = DevFlowRunStatus.Created,
            CurrentStage = DevFlowStage.UR,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync();

        return MapToResponse(run);
    }

    private static DevFlowRunResponse MapToResponse(DevFlowRun run) => new()
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
}
