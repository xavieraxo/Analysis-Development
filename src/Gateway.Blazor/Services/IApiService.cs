using Gateway.Blazor.Models;

namespace Gateway.Blazor.Services;

public interface IApiService
{
    Task<T?> GetAsync<T>(string endpoint);
    Task<T?> PostAsync<T>(string endpoint, object? data = null);
    Task<T?> PutAsync<T>(string endpoint, object? data = null);
    Task<T?> DeleteAsync<T>(string endpoint);
    Task<T?> PatchAsync<T>(string endpoint, object? data = null);
    Task<List<SystemConfiguration>> GetConfigurationsByTypeAsync(string type);
}

