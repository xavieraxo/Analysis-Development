using Data.Models;

namespace Gateway.Api.DTOs;

/// <summary>
/// Parámetros de consulta para listar DevFlow Runs.
/// </summary>
public class DevFlowRunsQueryParams
{
    public int? ProjectId { get; set; }
    public DevFlowRunStatus? Status { get; set; }
    public DevFlowStage? Stage { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
