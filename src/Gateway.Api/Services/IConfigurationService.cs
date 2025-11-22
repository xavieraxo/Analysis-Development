using Data.Models;
using Gateway.Api.DTOs;

namespace Gateway.Api.Services;

public interface IConfigurationService
{
    Task<List<SystemConfigurationDto>> GetAllConfigurationsAsync();
    Task<List<SystemConfigurationDto>> GetConfigurationsByTypeAsync(string configurationType);
    Task<SystemConfigurationDto?> GetConfigurationByKeyAsync(string configurationType, string key);
    Task<SystemConfigurationDto?> CreateConfigurationAsync(CreateSystemConfigurationRequest request, int userId);
    Task<SystemConfigurationDto?> UpdateConfigurationAsync(int id, UpdateSystemConfigurationRequest request, int userId);
    Task<bool> DeleteConfigurationAsync(int id);
    Task<bool> ToggleConfigurationAsync(int id, bool isActive, int userId);
    
    // Métodos específicos para respuestas automáticas
    Task<string?> GetAutoResponseAsync(string userMessage);
    
    // Métodos específicos para agentes (futuro)
    Task<bool> IsAgentEnabledAsync(string agentRole);
    Task<string?> GetAgentPersonalityAsync(string agentRole);
}

