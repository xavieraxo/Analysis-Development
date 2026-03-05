using Data.Models;
using Shared.Abstractions;

namespace Gateway.Api.Services;

/// <summary>
/// Implementación del pipeline DevFlow: UR → PM → PO → DEV.
/// </summary>
public sealed class DevFlowPipeline : IDevFlowPipeline
{
    private static readonly DevFlowStage[] OrderedStages = [DevFlowStage.UR, DevFlowStage.PM, DevFlowStage.PO, DevFlowStage.DEV];

    private static readonly IReadOnlyDictionary<DevFlowStage, AgentRole> StageToRole = new Dictionary<DevFlowStage, AgentRole>
    {
        [DevFlowStage.UR] = AgentRole.UR,
        [DevFlowStage.PM] = AgentRole.PM,
        [DevFlowStage.PO] = AgentRole.PO,
        [DevFlowStage.DEV] = AgentRole.Dev,
    };

    /// <inheritdoc />
    public DevFlowStage GetInitialStage() => DevFlowStage.UR;

    /// <inheritdoc />
    public DevFlowStage? GetNextStage(DevFlowStage current)
    {
        if (IsTerminal(current))
            return null;

        var idx = Array.IndexOf(OrderedStages, current);
        if (idx < 0 || idx >= OrderedStages.Length - 1)
            return null;

        return OrderedStages[idx + 1];
    }

    /// <inheritdoc />
    public bool IsTerminal(DevFlowStage stage) => stage == DevFlowStage.DEV;

    /// <inheritdoc />
    public bool IsValidTransition(DevFlowStage from, DevFlowStage to)
    {
        var next = GetNextStage(from);
        return next.HasValue && next.Value == to;
    }

    /// <inheritdoc />
    public AgentRole GetAgentRoleForStage(DevFlowStage stage)
    {
        if (!StageToRole.TryGetValue(stage, out var role))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Stage no mapeado a AgentRole.");
        return role;
    }
}
