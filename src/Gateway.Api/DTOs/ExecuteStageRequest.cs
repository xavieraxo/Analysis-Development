using Data.Models;

namespace Gateway.Api.DTOs;

/// <summary>
/// Request para ejecutar una etapa del DevFlow.
/// </summary>
public class ExecuteStageRequest
{
    /// <summary>
    /// Texto de entrada para el agente (idea o mensaje). Si está vacío, se usa Title + Description del run.
    /// </summary>
    public string? InputText { get; set; }

    /// <summary>
    /// Etapa específica a ejecutar. Si no se indica, se usa run.CurrentStage.
    /// </summary>
    public DevFlowStage? OverrideStage { get; set; }
}
