using System.ComponentModel.DataAnnotations;
using Shared.Abstractions;

namespace Gateway.Api.DTOs;

public class BehaviorUpsertRequest
{
    [Required]
    public AgentRole AgentRole { get; set; }

    [Required]
    [MaxLength(255)]
    public string Alias { get; set; } = string.Empty;

    [Required]
    [MinLength(1500)]
    public string Prompt { get; set; } = string.Empty;

    public List<BehaviorInstruction> Instructions { get; set; } = new();
}

