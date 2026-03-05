using Data.Models;
using Gateway.Api.Services;
using Shared.Abstractions;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests unitarios para DevFlowPipeline (UR → PM → PO → DEV).
/// </summary>
public class DevFlowPipelineTests
{
    private readonly IDevFlowPipeline _pipeline = new DevFlowPipeline();

    [Fact]
    public void GetInitialStage_DevuelveUR()
    {
        var initial = _pipeline.GetInitialStage();

        Assert.Equal(DevFlowStage.UR, initial);
    }

    [Theory]
    [InlineData(DevFlowStage.UR, DevFlowStage.PM)]
    [InlineData(DevFlowStage.PM, DevFlowStage.PO)]
    [InlineData(DevFlowStage.PO, DevFlowStage.DEV)]
    public void GetNextStage_TransicionesCorrectas(DevFlowStage current, DevFlowStage expectedNext)
    {
        var next = _pipeline.GetNextStage(current);

        Assert.NotNull(next);
        Assert.Equal(expectedNext, next.Value);
    }

    [Fact]
    public void GetNextStage_EnDEV_DevuelveNull()
    {
        var next = _pipeline.GetNextStage(DevFlowStage.DEV);

        Assert.Null(next);
    }

    [Theory]
    [InlineData(DevFlowStage.UR, false)]
    [InlineData(DevFlowStage.PM, false)]
    [InlineData(DevFlowStage.PO, false)]
    [InlineData(DevFlowStage.DEV, true)]
    public void IsTerminal_CoincideConDEV(DevFlowStage stage, bool expected)
    {
        var result = _pipeline.IsTerminal(stage);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(DevFlowStage.UR, DevFlowStage.PM, true)]
    [InlineData(DevFlowStage.PM, DevFlowStage.PO, true)]
    [InlineData(DevFlowStage.PO, DevFlowStage.DEV, true)]
    public void IsValidTransition_TransicionesValidas_DevuelveTrue(DevFlowStage from, DevFlowStage to, bool _)
    {
        Assert.True(_pipeline.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(DevFlowStage.UR, DevFlowStage.PO)]
    [InlineData(DevFlowStage.UR, DevFlowStage.DEV)]
    [InlineData(DevFlowStage.PM, DevFlowStage.UR)]
    [InlineData(DevFlowStage.PM, DevFlowStage.DEV)]
    [InlineData(DevFlowStage.DEV, DevFlowStage.UR)]
    [InlineData(DevFlowStage.UR, DevFlowStage.UR)]
    [InlineData(DevFlowStage.DEV, DevFlowStage.DEV)]
    public void IsValidTransition_TransicionesInvalidas_DevuelveFalse(DevFlowStage from, DevFlowStage to)
    {
        Assert.False(_pipeline.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(DevFlowStage.UR, AgentRole.UR)]
    [InlineData(DevFlowStage.PM, AgentRole.PM)]
    [InlineData(DevFlowStage.PO, AgentRole.PO)]
    [InlineData(DevFlowStage.DEV, AgentRole.Dev)]
    public void GetAgentRoleForStage_MapeoCorrecto(DevFlowStage stage, AgentRole expectedRole)
    {
        var role = _pipeline.GetAgentRoleForStage(stage);

        Assert.Equal(expectedRole, role);
    }
}
