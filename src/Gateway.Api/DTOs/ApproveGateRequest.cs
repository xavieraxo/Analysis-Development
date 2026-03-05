using Data.Models;

namespace Gateway.Api.DTOs;

/// <summary>
/// Request para aprobar o rechazar un gate del DevFlow.
/// </summary>
public class ApproveGateRequest
{
    /// <summary>
    /// Etapa del gate a aprobar/rechazar (UR, PM, PO, DEV).
    /// </summary>
    public DevFlowStage Stage { get; set; }

    /// <summary>
    /// true = aprobar, false = rechazar.
    /// </summary>
    public bool Approved { get; set; }

    /// <summary>
    /// Comentario opcional con el motivo.
    /// </summary>
    public string? Comment { get; set; }
}
