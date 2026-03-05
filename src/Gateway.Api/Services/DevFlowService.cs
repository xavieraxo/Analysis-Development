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

    public async Task<PagedResponse<DevFlowRunListItem>?> GetRunsAsync(DevFlowRunsQueryParams query)
    {
        if (query.PageSize < 1 || query.PageSize > 100)
            return null;
        if (query.Page < 1)
            return null;

        var q = _context.DevFlowRuns.AsNoTracking();

        if (query.ProjectId.HasValue)
            q = q.Where(r => r.ProjectId == query.ProjectId.Value);
        if (query.Status.HasValue)
            q = q.Where(r => r.Status == query.Status.Value);
        if (query.Stage.HasValue)
            q = q.Where(r => r.CurrentStage == query.Stage.Value);
        if (query.CreatedByUserId.HasValue)
            q = q.Where(r => r.CreatedByUserId == query.CreatedByUserId.Value);
        if (query.FromDate.HasValue)
            q = q.Where(r => r.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(r => r.CreatedAt <= query.ToDate.Value);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new DevFlowRunListItem
            {
                Id = r.Id,
                ProjectId = r.ProjectId,
                Title = r.Title,
                Status = r.Status,
                CurrentStage = r.CurrentStage,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ArtifactsCount = r.Artifacts.Count,
                GatesCount = r.Gates.Count
            })
            .ToListAsync();

        return new PagedResponse<DevFlowRunListItem>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            Total = total
        };
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
