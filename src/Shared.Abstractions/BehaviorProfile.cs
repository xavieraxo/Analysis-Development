namespace Shared.Abstractions;

public class BehaviorProfile
{
    public AgentRole Role { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public List<BehaviorInstruction> Instructions { get; set; } = new();
    public bool FromFallback { get; set; }
}

