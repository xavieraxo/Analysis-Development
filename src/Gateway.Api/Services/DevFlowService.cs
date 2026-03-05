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

    public async Task<DevFlowRunDetailResponse?> GetRunByIdAsync(int id)
    {
        var run = await _context.DevFlowRuns
            .AsNoTracking()
            .Include(r => r.Artifacts)
            .Include(r => r.Gates)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (run == null)
            return null;

        return MapToDetailResponse(run);
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

    private static DevFlowRunDetailResponse MapToDetailResponse(DevFlowRun run) => new()
    {
        Id = run.Id,
        ProjectId = run.ProjectId,
        Title = run.Title,
        Description = run.Description,
        Status = run.Status,
        CurrentStage = run.CurrentStage,
        CreatedByUserId = run.CreatedByUserId,
        CreatedAt = run.CreatedAt,
        UpdatedAt = run.UpdatedAt,
        Artifacts = run.Artifacts
            .OrderBy(a => a.Stage)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new DevFlowArtifactSummaryDto
            {
                Id = a.Id,
                Stage = a.Stage,
                AgentRole = a.AgentRole,
                Version = a.Version,
                CreatedAt = a.CreatedAt
            })
            .ToList(),
        Gates = run.Gates
            .OrderBy(g => g.Stage)
            .ThenBy(g => g.CreatedAt)
            .Select(g => new DevFlowGateSummaryDto
            {
                Id = g.Id,
                Stage = g.Stage,
                Status = g.Status,
                DecisionComment = g.DecisionComment,
                DecidedByUserId = g.DecidedByUserId,
                DecidedAt = g.DecidedAt,
                CreatedAt = g.CreatedAt
            })
            .ToList()
    };
}
