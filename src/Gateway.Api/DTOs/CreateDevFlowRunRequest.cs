using System.ComponentModel.DataAnnotations;

namespace Gateway.Api.DTOs;

/// <summary>
/// Request para crear un DevFlow Run.
/// </summary>
public class CreateDevFlowRunRequest
{
    /// <summary>
    /// ID del proyecto opcional al que se asocia el run.
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// Título del run (requerido).
    /// </summary>
    [Required(ErrorMessage = "El título es requerido")]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción o idea inicial del cambio.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
