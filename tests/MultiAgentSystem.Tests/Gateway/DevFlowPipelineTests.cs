using Data.Models;
using Gateway.Api.Services;
using Shared.Abstractions;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests unitarios para DevFlowPipeline por tipo de flujo.
/// </summary>
public class DevFlowPipelineTests
{
    private readonly IDevFlowPipeline _pipeline = new DevFlowPipeline();

    [Fact]
    public void GetInitialStage_Discovery_DevuelveUR()
    {
        var initial = _pipeline.GetInitialStage(DevFlowType.Discovery);

        Assert.Equal(DevFlowStage.UR, initial);
    }

    [Theory]
    [InlineData(DevFlowStage.UR, DevFlowStage.PM)]
    [InlineData(DevFlowStage.PM, DevFlowStage.PO)]
    [InlineData(DevFlowStage.PO, DevFlowStage.UX)]
    [InlineData(DevFlowStage.UX, DevFlowStage.PLAN)]
    public void GetNextStage_Discovery_TransicionesCorrectas(DevFlowStage current, DevFlowStage expectedNext)
    {
        var next = _pipeline.GetNextStage(DevFlowType.Discovery, current);

        Assert.NotNull(next);
        Assert.Equal(expectedNext, next.Value);
    }

    [Fact]
    public void GetNextStage_Discovery_EnPLAN_DevuelveNull()
    {
        var next = _pipeline.GetNextStage(DevFlowType.Discovery, DevFlowStage.PLAN);

        Assert.Null(next);
    }

    [Theory]
    [InlineData(DevFlowStage.UR, false)]
    [InlineData(DevFlowStage.PM, false)]
    [InlineData(DevFlowStage.PO, false)]
    [InlineData(DevFlowStage.UX, false)]
    [InlineData(DevFlowStage.PLAN, true)]
    public void IsTerminal_Discovery_CoincideConPLAN(DevFlowStage stage, bool expected)
    {
        var result = _pipeline.IsTerminal(DevFlowType.Discovery, stage);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(DevFlowStage.UR, DevFlowStage.PM, true)]
    [InlineData(DevFlowStage.PM, DevFlowStage.PO, true)]
    [InlineData(DevFlowStage.PO, DevFlowStage.UX, true)]
    [InlineData(DevFlowStage.UX, DevFlowStage.PLAN, true)]
    public void IsValidTransition_Discovery_TransicionesValidas_DevuelveTrue(DevFlowStage from, DevFlowStage to, bool _)
    {
        Assert.True(_pipeline.IsValidTransition(DevFlowType.Discovery, from, to));
    }

    [Theory]
    [InlineData(DevFlowStage.UR, DevFlowStage.PO)]
    [InlineData(DevFlowStage.UR, DevFlowStage.DEV)]
    [InlineData(DevFlowStage.PM, DevFlowStage.UR)]
    [InlineData(DevFlowStage.PM, DevFlowStage.DEV)]
    [InlineData(DevFlowStage.PLAN, DevFlowStage.UR)]
    [InlineData(DevFlowStage.UR, DevFlowStage.UR)]
    [InlineData(DevFlowStage.PLAN, DevFlowStage.PLAN)]
    public void IsValidTransition_Discovery_TransicionesInvalidas_DevuelveFalse(DevFlowStage from, DevFlowStage to)
    {
        Assert.False(_pipeline.IsValidTransition(DevFlowType.Discovery, from, to));
    }

    [Theory]
    [InlineData(DevFlowStage.UR, AgentRole.UR)]
    [InlineData(DevFlowStage.PM, AgentRole.PM)]
    [InlineData(DevFlowStage.PO, AgentRole.PO)]
    [InlineData(DevFlowStage.UX, AgentRole.UX)]
    [InlineData(DevFlowStage.PLAN, AgentRole.PM)]
    [InlineData(DevFlowStage.DEV, AgentRole.Dev)]
    public void GetAgentRoleForStage_MapeoCorrecto(DevFlowStage stage, AgentRole expectedRole)
    {
        var role = _pipeline.GetAgentRoleForStage(stage);

        Assert.Equal(expectedRole, role);
    }

    [Fact]
    public void GetPreviousStage_Discovery_EnUR_DevuelveNull()
    {
        var prev = _pipeline.GetPreviousStage(DevFlowType.Discovery, DevFlowStage.UR);

        Assert.Null(prev);
    }

    [Theory]
    [InlineData(DevFlowStage.PM, DevFlowStage.UR)]
    [InlineData(DevFlowStage.PO, DevFlowStage.PM)]
    [InlineData(DevFlowStage.UX, DevFlowStage.PO)]
    [InlineData(DevFlowStage.PLAN, DevFlowStage.UX)]
    public void GetPreviousStage_Discovery_TransicionesCorrectas(DevFlowStage current, DevFlowStage expectedPrev)
    {
        var prev = _pipeline.GetPreviousStage(DevFlowType.Discovery, current);

        Assert.NotNull(prev);
        Assert.Equal(expectedPrev, prev.Value);
    }

    [Theory]
    [InlineData(DevFlowStage.UR, DevFlowStage.PM)]
    [InlineData(DevFlowStage.PM, DevFlowStage.PO)]
    [InlineData(DevFlowStage.PO, DevFlowStage.DEV)]
    public void GetNextStage_AutoDev_ConservaFlujoActual(DevFlowStage current, DevFlowStage expectedNext)
    {
        var next = _pipeline.GetNextStage(DevFlowType.AutoDev, current);

        Assert.NotNull(next);
        Assert.Equal(expectedNext, next.Value);
    }

    [Fact]
    public void IsTerminal_AutoDev_ConservaDEVComoTerminal()
    {
        Assert.True(_pipeline.IsTerminal(DevFlowType.AutoDev, DevFlowStage.DEV));
    }
}
