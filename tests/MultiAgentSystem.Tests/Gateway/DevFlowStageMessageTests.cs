using Data;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MultiAgentSystem.Tests.Gateway;

/// <summary>
/// Tests de persistencia de DevFlowStageMessage (tarea 11.1.1, PLAN_DISCOVERY_MVP).
/// </summary>
public class DevFlowStageMessageTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public DevFlowStageMessageTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task StageMessage_SePersisteYRecuperaOrdenadoPorEtapa()
    {
        var run = await CreateRunAsync();

        _context.DevFlowStageMessages.AddRange(
            new DevFlowStageMessage
            {
                DevFlowRunId = run.Id,
                Stage = DevFlowStage.UR,
                Sender = StageMessageSender.User,
                Content = "Quiero una app de turnos para mi clínica",
                CreatedAt = DateTime.UtcNow.AddMinutes(-2)
            },
            new DevFlowStageMessage
            {
                DevFlowRunId = run.Id,
                Stage = DevFlowStage.UR,
                Sender = StageMessageSender.Agent,
                Content = "¿Quiénes serían los usuarios principales del sistema?",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            });
        await _context.SaveChangesAsync();

        var mensajes = await _context.DevFlowStageMessages
            .Where(m => m.DevFlowRunId == run.Id && m.Stage == DevFlowStage.UR)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, mensajes.Count);
        Assert.Equal(StageMessageSender.User, mensajes[0].Sender);
        Assert.Equal(StageMessageSender.Agent, mensajes[1].Sender);
        Assert.Contains("turnos", mensajes[0].Content);
    }

    [Fact]
    public async Task StageMessage_NavegacionDesdeRun_IncluyeMensajes()
    {
        var run = await CreateRunAsync();

        _context.DevFlowStageMessages.Add(new DevFlowStageMessage
        {
            DevFlowRunId = run.Id,
            Stage = DevFlowStage.UR,
            Sender = StageMessageSender.User,
            Content = "Mensaje de prueba"
        });
        await _context.SaveChangesAsync();

        var retrieved = await _context.DevFlowRuns
            .Include(r => r.StageMessages)
            .FirstAsync(r => r.Id == run.Id);

        Assert.Single(retrieved.StageMessages);
        Assert.Equal(DevFlowStage.UR, retrieved.StageMessages[0].Stage);
    }

    [Fact]
    public async Task StageMessage_BorrarRun_EliminaMensajesEnCascada()
    {
        var run = await CreateRunAsync();

        _context.DevFlowStageMessages.Add(new DevFlowStageMessage
        {
            DevFlowRunId = run.Id,
            Stage = DevFlowStage.UR,
            Sender = StageMessageSender.Agent,
            Content = "Mensaje que debe borrarse con el run"
        });
        await _context.SaveChangesAsync();

        _context.DevFlowRuns.Remove(run);
        await _context.SaveChangesAsync();

        var restantes = await _context.DevFlowStageMessages
            .Where(m => m.DevFlowRunId == run.Id)
            .CountAsync();

        Assert.Equal(0, restantes);
    }

    private async Task<DevFlowRun> CreateRunAsync()
    {
        var user = new ApplicationUser
        {
            UserName = $"stage-msg-{Guid.NewGuid():N}@test.local",
            Email = $"stage-msg-{Guid.NewGuid():N}@test.local",
            Name = "Test User",
            Role = UserRole.Final,
            IsActive = true
        };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var run = new DevFlowRun
        {
            Title = "Run conversacional",
            Description = "Run para tests de mensajes de etapa",
            FlowType = DevFlowType.Discovery,
            Status = DevFlowRunStatus.InProgress,
            CurrentStage = DevFlowStage.UR,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DevFlowRuns.Add(run);
        await _context.SaveChangesAsync();
        return run;
    }

    public void Dispose() => _context.Dispose();
}
