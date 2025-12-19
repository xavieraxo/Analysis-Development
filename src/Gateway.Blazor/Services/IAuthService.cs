using Gateway.Blazor.Models;

namespace Gateway.Blazor.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string email, string password);
    Task<bool> LogoutAsync();
    bool IsAuthenticated();
    Task<bool> IsAuthenticatedAsync(); // Versión asíncrona para evitar deadlocks
    string? GetToken();
    UserInfo? GetUser();
    string? GetUserRole();
}

