using Data;
using Data.Models;
using Gateway.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(ApplicationDbContext context, ILogger<ConfigurationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SystemConfigurationDto>> GetAllConfigurationsAsync()
    {
        var configs = await _context.SystemConfigurations
            .OrderBy(c => c.ConfigurationType)
            .ThenBy(c => c.Priority)
            .ThenBy(c => c.Key)
            .ToListAsync();

        return configs.Select(MapToDto).ToList();
    }

    public async Task<List<SystemConfigurationDto>> GetConfigurationsByTypeAsync(string configurationType)
    {
        var configs = await _context.SystemConfigurations
            .Where(c => c.ConfigurationType == configurationType && c.IsActive)
            .OrderByDescending(c => c.Priority)
            .ThenBy(c => c.Key)
            .ToListAsync();

        return configs.Select(MapToDto).ToList();
    }

    public async Task<SystemConfigurationDto?> GetConfigurationByKeyAsync(string configurationType, string key)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.ConfigurationType == configurationType && c.Key == key && c.IsActive);

        return config == null ? null : MapToDto(config);
    }

    public async Task<SystemConfigurationDto?> CreateConfigurationAsync(CreateSystemConfigurationRequest request, int userId)
    {
        // Verificar si ya existe
        var exists = await _context.SystemConfigurations
            .AnyAsync(c => c.ConfigurationType == request.ConfigurationType && c.Key == request.Key);

        if (exists)
        {
            _logger.LogWarning($"Configuración ya existe: {request.ConfigurationType}/{request.Key}");
            return null;
        }

        var config = new SystemConfiguration
        {
            ConfigurationType = request.ConfigurationType,
            Key = request.Key,
            Value = request.Value,
            Description = request.Description,
            IsActive = request.IsActive,
            MatchPattern = request.MatchPattern,
            Priority = request.Priority,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Configuración creada: {config.ConfigurationType}/{config.Key} por usuario {userId}");
        return MapToDto(config);
    }

    public async Task<SystemConfigurationDto?> UpdateConfigurationAsync(int id, UpdateSystemConfigurationRequest request, int userId)
    {
        var config = await _context.SystemConfigurations.FindAsync(id);
        if (config == null)
        {
            return null;
        }

        if (request.Value != null) config.Value = request.Value;
        if (request.Description != null) config.Description = request.Description;
        if (request.IsActive.HasValue) config.IsActive = request.IsActive.Value;
        if (request.MatchPattern != null) config.MatchPattern = request.MatchPattern;
        if (request.Priority.HasValue) config.Priority = request.Priority.Value;

        config.UpdatedByUserId = userId;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Configuración actualizada: {config.ConfigurationType}/{config.Key} por usuario {userId}");
        return MapToDto(config);
    }

    public async Task<bool> DeleteConfigurationAsync(int id)
    {
        var config = await _context.SystemConfigurations.FindAsync(id);
        if (config == null)
        {
            return false;
        }

        _context.SystemConfigurations.Remove(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Configuración eliminada: {config.ConfigurationType}/{config.Key}");
        return true;
    }

    public async Task<bool> ToggleConfigurationAsync(int id, bool isActive, int userId)
    {
        var config = await _context.SystemConfigurations.FindAsync(id);
        if (config == null)
        {
            return false;
        }

        config.IsActive = isActive;
        config.UpdatedByUserId = userId;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GetAutoResponseAsync(string userMessage)
    {
        // Buscar respuestas automáticas activas ordenadas por prioridad
        var autoResponses = await _context.SystemConfigurations
            .Where(c => c.ConfigurationType == "AutoResponse" && c.IsActive)
            .OrderByDescending(c => c.Priority)
            .ToListAsync();

        var normalizedMessage = userMessage.Trim().ToLowerInvariant();

        foreach (var response in autoResponses)
        {
            // Si tiene patrón de coincidencia, verificar con regex
            if (!string.IsNullOrEmpty(response.MatchPattern))
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(
                        response.MatchPattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | 
                        System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                    
                    if (regex.IsMatch(normalizedMessage))
                    {
                        _logger.LogInformation($"Respuesta automática encontrada: {response.Key} para mensaje: {userMessage}");
                        return response.Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error en patrón regex de configuración {response.Key}: {response.MatchPattern}");
                }
            }
            else
            {
                // Coincidencia exacta con la clave (normalizada)
                if (normalizedMessage == response.Key.ToLowerInvariant())
                {
                    _logger.LogInformation($"Respuesta automática encontrada: {response.Key} para mensaje: {userMessage}");
                    return response.Value;
                }
            }
        }

        return null; // No se encontró respuesta automática
    }

    public async Task<bool> IsAgentEnabledAsync(string agentRole)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => 
                c.ConfigurationType == "AgentEnabled" && 
                c.Key == agentRole && 
                c.IsActive);

        if (config == null)
        {
            // Por defecto, todos los agentes están habilitados si no hay configuración
            return true;
        }

        return bool.TryParse(config.Value, out var enabled) && enabled;
    }

    public async Task<string?> GetAgentPersonalityAsync(string agentRole)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => 
                c.ConfigurationType == "AgentPersonality" && 
                c.Key == agentRole && 
                c.IsActive);

        return config?.Value;
    }

    private static SystemConfigurationDto MapToDto(SystemConfiguration config)
    {
        return new SystemConfigurationDto
        {
            Id = config.Id,
            ConfigurationType = config.ConfigurationType,
            Key = config.Key,
            Value = config.Value,
            Description = config.Description,
            IsActive = config.IsActive,
            MatchPattern = config.MatchPattern,
            Priority = config.Priority,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            CreatedByUserId = config.CreatedByUserId,
            UpdatedByUserId = config.UpdatedByUserId
        };
    }
}

