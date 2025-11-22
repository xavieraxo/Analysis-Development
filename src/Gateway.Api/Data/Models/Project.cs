using Shared.Abstractions;

namespace Data.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.InProgress;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Datos del análisis MVP
    public string? ConversationId { get; set; }
    public string? Summary { get; set; }
    public string? InternalConversationsJson { get; set; } // JSON de las conversaciones internas
    
    // Relaciones
    public List<ProjectLog> Logs { get; set; } = new();
}

public enum ProjectStatus
{
    InProgress = 0,
    Completed = 1,
    Cancelled = 2
}

