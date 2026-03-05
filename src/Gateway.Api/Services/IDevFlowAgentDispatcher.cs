using Data.Models;
using Gateway.Api.DTOs;
using Shared.Abstractions;

namespace Gateway.Api.Services;

/// <summary>
/// Despacha y ejecuta agentes por etapa del DevFlow.
/// Resuelve el agente a partir del stage y ejecuta con input consistente.
/// </summary>
public interface IDevFlowAgentDispatcher
{
    /// <summary>
    /// Resuelve el agente que corresponde a la etapa indicada.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si el stage no tiene agente registrado.</exception>
    IAgent ResolveAgent(DevFlowStage stage);

    /// <summary>
    /// Ejecuta el agente correspondiente a la etapa con el input proporcionado.
    /// </summary>
    /// <param name="stage">Etapa del DevFlow (UR, PM, PO, DEV).</param>
    /// <param name="input">Input para el agente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado de la ejecución (PayloadJson listo para DevFlowArtifact).</returns>
    /// <exception cref="InvalidOperationException">Si el stage no tiene agente registrado.</exception>
    Task<DevFlowAgentResult> ExecuteAsync(
        DevFlowStage stage,
        DevFlowAgentInput input,
        CancellationToken cancellationToken = default);
}
