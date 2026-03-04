namespace Gateway.Blazor.Models;

public class Behavior
{
    public AgentRole AgentRole { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public List<BehaviorInstruction> Instructions { get; set; } = new();
    public bool FromFallback { get; set; }
}

