using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Abstractions;

namespace Data.Models;

public class Behavior
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public AgentRole AgentRole { get; set; }

    [Required]
    [MaxLength(255)]
    public string Alias { get; set; } = string.Empty;

    [Required]
    public string Prompt { get; set; } = string.Empty;

    [Required]
    public string InstructionsJson { get; set; } = "[]";
}

