using Data.Models;
using Shared.Abstractions;

namespace Gateway.Api.Services;

/// <summary>
/// Define el orden y la lógica mínima del flujo DevFlow (UR → PM → PO → DEV).
/// </summary>
public interface IDevFlowPipeline
{
    /// <summary>
    /// Obtiene la etapa inicial del pipeline (siempre UR).
    /// </summary>
    DevFlowStage GetInitialStage();

    /// <summary>
    /// Obtiene la siguiente etapa. Devuelve null si current es DEV (terminal).
    /// </summary>
    DevFlowStage? GetNextStage(DevFlowStage current);

    /// <summary>
    /// Indica si la etapa es terminal (no hay siguiente). Solo DEV es terminal.
    /// </summary>
    bool IsTerminal(DevFlowStage stage);

    /// <summary>
    /// Valida si la transición entre dos etapas es válida según el orden del pipeline.
    /// </summary>
    bool IsValidTransition(DevFlowStage from, DevFlowStage to);

    /// <summary>
    /// Obtiene el AgentRole correspondiente a una etapa del pipeline.
    /// </summary>
    AgentRole GetAgentRoleForStage(DevFlowStage stage);

    /// <summary>
    /// Obtiene la etapa anterior. Devuelve null para UR (primera etapa).
    /// </summary>
    DevFlowStage? GetPreviousStage(DevFlowStage current);
}
