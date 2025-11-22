namespace Data.Models;

/// <summary>
/// Configuración del sistema para respuestas automáticas, personalidades de agentes, etc.
/// </summary>
public class SystemConfiguration
{
    public int Id { get; set; }
    
    /// <summary>
    /// Tipo de configuración (AutoResponse, AgentPersonality, AgentEnabled, etc.)
    /// </summary>
    public string ConfigurationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Clave única de la configuración (ej: "greeting_hello", "agent_pm_personality")
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Valor de la configuración (JSON o texto)
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Descripción de la configuración
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Si está activa o no
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Patrón de coincidencia para respuestas automáticas (regex opcional)
    /// </summary>
    public string? MatchPattern { get; set; }
    
    /// <summary>
    /// Prioridad (mayor número = mayor prioridad)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Fecha de creación
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Usuario que creó la configuración
    /// </summary>
    public int CreatedByUserId { get; set; }
    
    /// <summary>
    /// Usuario que actualizó por última vez
    /// </summary>
    public int? UpdatedByUserId { get; set; }
}

