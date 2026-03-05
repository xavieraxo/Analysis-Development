using Data.Models;

namespace Gateway.Api.DTOs;

/// <summary>
/// Respuesta de aprobación/rechazo de gate del DevFlow.
/// </summary>
public class ApproveGateResponse
{
    public DevFlowGateSummaryDto Gate { get; set; } = null!;
    public DevFlowRunStatus Status { get; set; }
    public DevFlowStage? CurrentStage { get; set; }
}
