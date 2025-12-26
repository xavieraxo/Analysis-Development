using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Gateway.Blazor.Models;

namespace Gateway.Blazor.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string endpoint, object? data = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        
        if (await _authService.IsAuthenticatedAsync())
        {
            var token = _authService.GetToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (data != null)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await Task.FromResult(request);
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(request);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            throw new UnauthorizedAccessException("Sesión expirada");
        }

        response.EnsureSuccessStatusCode();
        
        if (response.Content.Headers.ContentLength == 0)
            return default;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, endpoint, data);
        var response = await _httpClient.SendAsync(request);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            throw new UnauthorizedAccessException("Sesión expirada");
        }

        response.EnsureSuccessStatusCode();
        
        if (response.Content.Headers.ContentLength == 0)
            return default;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object? data = null)
    {
        var request = await CreateRequestAsync(HttpMethod.Put, endpoint, data);
        var response = await _httpClient.SendAsync(request);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            throw new UnauthorizedAccessException("Sesión expirada");
        }

        response.EnsureSuccessStatusCode();
        
        if (response.Content.Headers.ContentLength == 0)
            return default;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    public async Task<T?> DeleteAsync<T>(string endpoint)
    {
        var request = await CreateRequestAsync(HttpMethod.Delete, endpoint);
        var response = await _httpClient.SendAsync(request);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            throw new UnauthorizedAccessException("Sesión expirada");
        }

        response.EnsureSuccessStatusCode();
        
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || 
            response.Content.Headers.ContentLength == 0)
            return default;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    public async Task<T?> PatchAsync<T>(string endpoint, object? data = null)
    {
        var request = await CreateRequestAsync(HttpMethod.Patch, endpoint, data);
        var response = await _httpClient.SendAsync(request);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            throw new UnauthorizedAccessException("Sesión expirada");
        }

        response.EnsureSuccessStatusCode();
        
        if (response.Content.Headers.ContentLength == 0)
            return default;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    public async Task<List<SystemConfiguration>> GetConfigurationsByTypeAsync(string type)
    {
        return await GetAsync<List<SystemConfiguration>>($"/api/configurations/{type}") ?? new List<SystemConfiguration>();
    }
}

