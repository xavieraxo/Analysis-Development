using System.Text.Json.Serialization;
using Gateway.Blazor.Converters;

namespace Gateway.Blazor.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    [JsonConverter(typeof(ProjectStatusJsonConverter))]
    public string Status { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int UserId { get; set; }
    public string? ConversationId { get; set; }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

