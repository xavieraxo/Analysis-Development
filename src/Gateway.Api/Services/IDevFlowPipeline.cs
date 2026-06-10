using Data.Models;
using Shared.Abstractions;

namespace Gateway.Api.Services;

/// <summary>
/// Define el orden y la lógica mínima del flujo DevFlow según tipo de flujo.
/// </summary>
public interface IDevFlowPipeline
{
    /// <summary>
    /// Obtiene la etapa inicial del pipeline.
    /// </summary>
    DevFlowStage GetInitialStage(DevFlowType flowType);

    /// <summary>
    /// Obtiene la siguiente etapa. Devuelve null si current es terminal.
    /// </summary>
    DevFlowStage? GetNextStage(DevFlowType flowType, DevFlowStage current);

    /// <summary>
    /// Indica si la etapa es terminal (no hay siguiente).
    /// </summary>
    bool IsTerminal(DevFlowType flowType, DevFlowStage stage);

    /// <summary>
    /// Valida si la transición entre dos etapas es válida según el orden del pipeline.
    /// </summary>
    bool IsValidTransition(DevFlowType flowType, DevFlowStage from, DevFlowStage to);

    /// <summary>
    /// Obtiene el AgentRole correspondiente a una etapa del pipeline.
    /// </summary>
    AgentRole GetAgentRoleForStage(DevFlowStage stage);

    /// <summary>
    /// Obtiene la etapa anterior. Devuelve null para la primera etapa.
    /// </summary>
    DevFlowStage? GetPreviousStage(DevFlowType flowType, DevFlowStage current);

    /// <summary>
    /// Obtiene el orden completo de etapas para un tipo de flujo.
    /// </summary>
    IReadOnlyList<DevFlowStage> GetStages(DevFlowType flowType);
}
