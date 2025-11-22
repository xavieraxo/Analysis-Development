using System.Text.Json;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;

namespace Gateway.Api.Services;

public interface IAgentLoggingService : Orchestrator.App.ILoggingCallback
{
    Task<List<AgentLogEntry>> GetConversationLogsAsync(string conversationId);
    Task<List<AgentLogEntry>> GetAllLogsAsync(int? limit = null);
}

public class AgentLoggingService : IAgentLoggingService
{
    private readonly Data.ApplicationDbContext _context;
    private readonly ILogger<AgentLoggingService> _logger;
    private readonly string _logsDirectory;

    public AgentLoggingService(
        Data.ApplicationDbContext context,
        ILogger<AgentLoggingService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _logsDirectory = Path.Combine(environment.ContentRootPath, "logs", "conversations");
        Directory.CreateDirectory(_logsDirectory);
    }

    public void LogAgentInteraction(string conversationId, AgentRole agentRole, string message, string? context = null)
    {
        var logEntry = new AgentLogEntry
        {
            ConversationId = conversationId,
            AgentRole = agentRole.ToString(),
            Message = message,
            Context = context,
            Timestamp = DateTime.UtcNow
        };

        // 1. Log en consola (con colores)
        var color = GetColorForAgent(agentRole);
        _logger.LogInformation(
            "[{Agent}] [{ConversationId}] {Message}",
            agentRole,
            conversationId,
            message);

        // 2. Guardar en archivo JSON
        SaveToFile(conversationId, logEntry);

        // 3. Guardar en base de datos (si hay proyecto asociado)
        _ = Task.Run(async () =>
        {
            try
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.ConversationId == conversationId);

                if (project != null)
                {
                    var projectLog = new ProjectLog
                    {
                        ProjectId = project.Id,
                        AgentRole = agentRole.ToString(),
                        Message = message,
                        Metadata = context != null ? JsonSerializer.Serialize(new { Context = context }) : null
                    };

                    _context.ProjectLogs.Add(projectLog);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving log to database");
            }
        });
    }

    public async Task<List<AgentLogEntry>> GetConversationLogsAsync(string conversationId)
    {
        var filePath = Path.Combine(_logsDirectory, $"{conversationId}.json");
        if (!File.Exists(filePath))
        {
            return new List<AgentLogEntry>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var logs = JsonSerializer.Deserialize<List<AgentLogEntry>>(json);
            return logs ?? new List<AgentLogEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading conversation logs");
            return new List<AgentLogEntry>();
        }
    }

    public async Task<List<AgentLogEntry>> GetAllLogsAsync(int? limit = null)
    {
        var allLogs = new List<AgentLogEntry>();

        try
        {
            var files = Directory.GetFiles(_logsDirectory, "*.json")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            if (limit.HasValue)
            {
                files = files.Take(limit.Value).ToList();
            }

            foreach (var file in files)
            {
                var logs = await GetConversationLogsAsync(Path.GetFileNameWithoutExtension(file));
                allLogs.AddRange(logs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading all logs");
        }

        return allLogs.OrderByDescending(l => l.Timestamp).ToList();
    }

    private void SaveToFile(string conversationId, AgentLogEntry entry)
    {
        var filePath = Path.Combine(_logsDirectory, $"{conversationId}.json");
        var logs = new List<AgentLogEntry>();

        if (File.Exists(filePath))
        {
            try
            {
                var existingJson = File.ReadAllText(filePath);
                logs = JsonSerializer.Deserialize<List<AgentLogEntry>>(existingJson) ?? new List<AgentLogEntry>();
            }
            catch
            {
                logs = new List<AgentLogEntry>();
            }
        }

        logs.Add(entry);

        var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private static string GetColorForAgent(AgentRole role)
    {
        return role switch
        {
            AgentRole.UR => "CYAN",
            AgentRole.PM => "GREEN",
            AgentRole.PO => "YELLOW",
            AgentRole.UX => "MAGENTA",
            AgentRole.Dev => "BLUE",
            _ => "WHITE"
        };
    }
}

public class AgentLogEntry
{
    public string ConversationId { get; set; } = string.Empty;
    public string AgentRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
    public DateTime Timestamp { get; set; }
}

