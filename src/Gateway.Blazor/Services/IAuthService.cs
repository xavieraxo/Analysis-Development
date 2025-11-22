using Gateway.Blazor.Models;

namespace Gateway.Blazor.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string email, string password);
    Task<bool> LogoutAsync();
    bool IsAuthenticated();
    string? GetToken();
    UserInfo? GetUser();
    string? GetUserRole();
}

