using Data;
using Data.Models;
using Gateway.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Services;

public class DevFlowService : IDevFlowService
{
    private readonly ApplicationDbContext _context;
    private readonly IDevFlowAgentDispatcher _dispatcher;
    private readonly IDevFlowPipeline _pipeline;

    public DevFlowService(ApplicationDbContext context, IDevFlowAgentDispatcher dispatcher, IDevFlowPipeline pipeline)
    {
        _context = context;
        _dispatcher = dispatcher;
        _pipeline = pipeline;
    }

    public async Task<DevFlowRunResponse?> CreateRunAsync(CreateDevFlowRunRequest request, int createdByUserId)
    {
        var run = new DevFlowRun
        {
            ProjectId = request.ProjectId ?? throw new InvalidOperationException("ProjectId is required to create a DevFlow run."),
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

    public async Task<ExecuteStageResult> ExecuteStageAsync(int runId, ExecuteStageRequest request, CancellationToken cancellationToken = default)
    {
        var run = await _context.DevFlowRuns
            .Include(r => r.Artifacts)
            .Include(r => r.Gates)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (run == null)
            return ExecuteStageResult.NotFound("El run no existe.");

        if (run.Status == DevFlowRunStatus.Completed || run.Status == DevFlowRunStatus.Cancelled)
            return ExecuteStageResult.BadRequest($"El run está en estado {run.Status} y no puede ejecutar más etapas.");

        var stage = request.OverrideStage ?? run.CurrentStage ?? _pipeline.GetInitialStage();

        if (!Enum.IsDefined(typeof(DevFlowStage), stage))
            return ExecuteStageResult.BadRequest("Etapa inválida.");

        var previousStage = _pipeline.GetPreviousStage(stage);
        if (previousStage.HasValue)
        {
            var blockingGate = run.Gates.FirstOrDefault(g => g.Stage == previousStage.Value);
            if (blockingGate != null && blockingGate.Status == DevFlowGateStatus.Pending)
                return ExecuteStageResult.Conflict($"El gate para la etapa {previousStage.Value} está pendiente de aprobación. Debe aprobar antes de ejecutar {stage}.");
        }

        var userMessage = !string.IsNullOrWhiteSpace(request.InputText)
            ? request.InputText!.Trim()
            : $"{run.Title}\n\n{run.Description}".Trim();

        var previousArtifacts = run.Artifacts
            .Where(a => (int)a.Stage < (int)stage)
            .OrderBy(a => a.Stage)
            .ThenBy(a => a.CreatedAt);
        var previousSummary = string.Join("\n\n", previousArtifacts.Select(a => $"[{a.Stage}] {a.PayloadJson}"));

        var agentInput = new DevFlowAgentInput
        {
            RunId = run.Id,
            ProjectId = run.ProjectId,
            ConversationId = $"devflow-{run.Id}",
            UserMessage = userMessage,
            PreviousArtifactsSummary = previousSummary
        };

        var result = await _dispatcher.ExecuteAsync(stage, agentInput, cancellationToken);

        var artifact = new DevFlowArtifact
        {
            DevFlowRunId = run.Id,
            Stage = stage,
            AgentRole = result.AgentRole,
            PayloadJson = result.PayloadJson,
            Version = result.Version,
            CreatedAt = DateTime.UtcNow
        };
        run.Artifacts.Add(artifact);

        var nextStage = _pipeline.GetNextStage(stage);
        if (nextStage.HasValue)
        {
            run.CurrentStage = nextStage.Value;
            run.Status = DevFlowRunStatus.InProgress;
            var gate = new DevFlowGate
            {
                DevFlowRunId = run.Id,
                Stage = stage,
                Status = DevFlowGateStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            run.Gates.Add(gate);
        }
        else
        {
            run.CurrentStage = stage;
            run.Status = DevFlowRunStatus.Completed;
        }

        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var runDetail = MapToDetailResponse(run);
        var artifactDto = new ExecuteStageArtifactDto
        {
            Id = artifact.Id,
            Stage = artifact.Stage,
            AgentRole = artifact.AgentRole,
            Version = artifact.Version,
            CreatedAt = artifact.CreatedAt
        };

        return ExecuteStageResult.Success(new ExecuteStageResponse
        {
            Run = runDetail,
            Artifact = artifactDto
        });
    }

    public async Task<ApproveGateResult> ApproveGateAsync(int runId, ApproveGateRequest request, int decidedByUserId, CancellationToken cancellationToken = default)
    {
        var run = await _context.DevFlowRuns
            .Include(r => r.Gates)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (run == null)
            return ApproveGateResult.NotFound("El run no existe.");

        if (run.Status == DevFlowRunStatus.Completed)
            return ApproveGateResult.BadRequest("No se puede aprobar/rechazar un run ya completado.");

        if (!Enum.IsDefined(typeof(DevFlowStage), request.Stage))
            return ApproveGateResult.BadRequest("Etapa inválida.");

        var gate = run.Gates.FirstOrDefault(g => g.Stage == request.Stage);
        if (gate == null)
        {
            gate = new DevFlowGate
            {
                DevFlowRunId = run.Id,
                Stage = request.Stage,
                Status = DevFlowGateStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            run.Gates.Add(gate);
        }

        gate.Status = request.Approved ? DevFlowGateStatus.Approved : DevFlowGateStatus.Rejected;
        gate.DecisionComment = request.Comment?.Trim();
        gate.DecidedByUserId = decidedByUserId;
        gate.DecidedAt = DateTime.UtcNow;

        if (!request.Approved)
        {
            run.Status = DevFlowRunStatus.Cancelled;
        }

        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var gateDto = new DevFlowGateSummaryDto
        {
            Id = gate.Id,
            Stage = gate.Stage,
            Status = gate.Status,
            DecisionComment = gate.DecisionComment,
            DecidedByUserId = gate.DecidedByUserId,
            DecidedAt = gate.DecidedAt,
            CreatedAt = gate.CreatedAt
        };

        return ApproveGateResult.Success(new ApproveGateResponse
        {
            Gate = gateDto,
            Status = run.Status,
            CurrentStage = run.CurrentStage
        });
    }

    public async Task<BranchPlanExportDto?> GetBranchPlanExportAsync(int runId, CancellationToken cancellationToken = default)
    {
        var plan = await _context.BranchPlans
            .AsNoTracking()
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.DevFlowRunId == runId, cancellationToken);

        if (plan == null)
            return null;

        return new BranchPlanExportDto
        {
            RunId = plan.DevFlowRunId,
            BranchPlanId = plan.Id,
            Items = plan.Items
                .OrderBy(i => i.Order)
                .Select(i => new BranchPlanItemExportDto
                {
                    Order = i.Order,
                    StoryId = i.StoryId,
                    TaskId = i.TaskId,
                    Area = i.Area,
                    BranchName = i.BranchName,
                    Title = i.Title,
                    Description = i.Description
                })
                .ToList()
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
