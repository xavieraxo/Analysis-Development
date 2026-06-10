using Data.Models;
using Shared.Abstractions;

namespace Gateway.Api.Services;

/// <summary>
/// Implementación del pipeline DevFlow por tipo de flujo.
/// </summary>
public sealed class DevFlowPipeline : IDevFlowPipeline
{
    private static readonly IReadOnlyDictionary<DevFlowType, DevFlowStage[]> OrderedStagesByFlowType =
        new Dictionary<DevFlowType, DevFlowStage[]>
        {
            [DevFlowType.Discovery] = [DevFlowStage.UR, DevFlowStage.PM, DevFlowStage.PO, DevFlowStage.UX, DevFlowStage.PLAN],
            [DevFlowType.AutoDev] = [DevFlowStage.UR, DevFlowStage.PM, DevFlowStage.PO, DevFlowStage.DEV],
            [DevFlowType.Development] = [DevFlowStage.UR, DevFlowStage.PM, DevFlowStage.PO, DevFlowStage.DEV]
        };

    private static readonly IReadOnlyDictionary<DevFlowStage, AgentRole> StageToRole = new Dictionary<DevFlowStage, AgentRole>
    {
        [DevFlowStage.UR] = AgentRole.UR,
        [DevFlowStage.PM] = AgentRole.PM,
        [DevFlowStage.PO] = AgentRole.PO,
        [DevFlowStage.UX] = AgentRole.UX,
        [DevFlowStage.PLAN] = AgentRole.PM,
        [DevFlowStage.DEV] = AgentRole.Dev,
    };

    public IReadOnlyList<DevFlowStage> GetStages(DevFlowType flowType)
    {
        if (!OrderedStagesByFlowType.TryGetValue(flowType, out var stages))
            throw new ArgumentOutOfRangeException(nameof(flowType), flowType, "FlowType no soportado.");
        return stages;
    }

    /// <inheritdoc />
    public DevFlowStage GetInitialStage(DevFlowType flowType) => GetStages(flowType)[0];

    /// <inheritdoc />
    public DevFlowStage? GetNextStage(DevFlowType flowType, DevFlowStage current)
    {
        var orderedStages = GetStages(flowType);
        if (IsTerminal(flowType, current))
            return null;

        var idx = FindStageIndex(orderedStages, current);
        if (idx < 0 || idx >= orderedStages.Count - 1)
            return null;

        return orderedStages[idx + 1];
    }

    /// <inheritdoc />
    public bool IsTerminal(DevFlowType flowType, DevFlowStage stage)
    {
        var orderedStages = GetStages(flowType);
        return orderedStages.Count > 0 && orderedStages[^1] == stage;
    }

    /// <inheritdoc />
    public bool IsValidTransition(DevFlowType flowType, DevFlowStage from, DevFlowStage to)
    {
        var next = GetNextStage(flowType, from);
        return next.HasValue && next.Value == to;
    }

    /// <inheritdoc />
    public AgentRole GetAgentRoleForStage(DevFlowStage stage)
    {
        if (!StageToRole.TryGetValue(stage, out var role))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Stage no mapeado a AgentRole.");
        return role;
    }

    /// <inheritdoc />
    public DevFlowStage? GetPreviousStage(DevFlowType flowType, DevFlowStage current)
    {
        var orderedStages = GetStages(flowType);
        if (orderedStages.Count == 0 || orderedStages[0] == current)
            return null;
        var idx = FindStageIndex(orderedStages, current);
        if (idx <= 0 || idx >= orderedStages.Count)
            return null;
        return orderedStages[idx - 1];
    }

    private static int FindStageIndex(IReadOnlyList<DevFlowStage> stages, DevFlowStage stage)
    {
        for (var i = 0; i < stages.Count; i++)
        {
            if (stages[i] == stage)
                return i;
        }

        return -1;
    }
}
