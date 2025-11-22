using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Gateway.Blazor.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace Gateway.Blazor.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private LoginResponse? _currentUser;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, NavigationManager navigationManager)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
        // Cargar desde storage de forma asíncrona sin bloquear
        _ = Task.Run(async () => await LoadUserFromStorageAsync());
    }

    private async Task LoadUserFromStorageAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "token");
            var userJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "user");
            var loginTimestampStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "loginTimestamp");
            
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
            {
                // Verificar expiración de 30 minutos
                if (!string.IsNullOrEmpty(loginTimestampStr) && long.TryParse(loginTimestampStr, out var loginTimestamp))
                {
                    var loginTime = DateTimeOffset.FromUnixTimeMilliseconds(loginTimestamp);
                    var now = DateTimeOffset.UtcNow;
                    var elapsed = now - loginTime;
                    
                    // Si han pasado más de 30 minutos, limpiar sesión
                    if (elapsed.TotalMinutes > 30)
                    {
                        ClearStorage();
                        return;
                    }
                }
                else
                {
                    // Si no hay timestamp, limpiar (sesión antigua)
                    ClearStorage();
                    return;
                }
                
                var user = System.Text.Json.JsonSerializer.Deserialize<UserInfo>(userJson);
                if (user != null && !IsTokenExpired(token))
                {
                    _currentUser = new LoginResponse { Token = token, User = user };
                }
                else
                {
                    // Si el token expiró, limpiar el almacenamiento
                    ClearStorage();
                }
            }
        }
        catch
        {
            // Ignorar errores al cargar desde localStorage
        }
    }
    
    private void ClearStorage()
    {
        try
        {
            _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "token").GetAwaiter().GetResult();
            _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "user").GetAwaiter().GetResult();
            _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "loginTimestamp").GetAwaiter().GetResult();
        }
        catch
        {
            // Ignorar errores al limpiar
        }
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            return jsonToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        // El email puede ser un hash de superusuario o un email normal
        var request = new LoginRequest
        {
            Email = email?.Trim() ?? string.Empty,
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            // Si es 401 (Unauthorized), lanzar excepción específica para credenciales incorrectas
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("El usuario o la contraseña no coinciden");
            }
            throw new Exception($"Error en login: {response.StatusCode} - {error}");
        }

        var content = await response.Content.ReadAsStringAsync();
        
        // Deserializar usando JsonDocument para manejar la estructura del backend
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        if (!root.TryGetProperty("token", out var tokenElement) || !root.TryGetProperty("user", out var userElement))
        {
            throw new Exception("Respuesta de login con formato inválido");
        }
        
        var token = tokenElement.GetString() ?? string.Empty;
        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Token vacío en la respuesta");
        }
        
        // Mapear UserDto del backend a UserInfo del frontend
        var userInfo = new UserInfo
        {
            Id = userElement.TryGetProperty("id", out var idElement) ? idElement.GetInt32() : 0,
            Email = userElement.TryGetProperty("email", out var emailElement) ? emailElement.GetString() ?? string.Empty : string.Empty,
            Name = userElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty,
            Role = userElement.TryGetProperty("role", out var roleElement) ? roleElement.GetString() ?? string.Empty : string.Empty
        };
        
        var loginResponse = new LoginResponse
        {
            Token = token,
            User = userInfo
        };
        
        _currentUser = loginResponse;
        var loginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "token", loginResponse.Token);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "user", 
            System.Text.Json.JsonSerializer.Serialize(loginResponse.User));
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "loginTimestamp", loginTimestamp.ToString());

        return loginResponse;
    }

    public async Task<bool> LogoutAsync()
    {
        _currentUser = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "token");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "user");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "loginTimestamp");
        _navigationManager.NavigateTo("/login");
        return true;
    }

    public bool IsAuthenticated()
    {
        // Si _currentUser está en memoria, verificar directamente
        if (_currentUser != null)
        {
            // Verificar que el token no esté vacío
            if (string.IsNullOrEmpty(_currentUser.Token))
                return false;
            
            // Verificar expiración de 30 minutos
            return CheckSessionExpiration();
        }
        
        // Si no está en memoria, intentar cargar desde storage de forma síncrona
        // Nota: Esto puede fallar durante el renderizado inicial, por lo que debe estar en un try-catch
        try
        {
            // Verificar si IJSRuntime está disponible antes de usarlo
            if (_jsRuntime == null)
                return false;
                
            var token = _jsRuntime.InvokeAsync<string>("localStorage.getItem", "token").GetAwaiter().GetResult();
            var userJson = _jsRuntime.InvokeAsync<string>("localStorage.getItem", "user").GetAwaiter().GetResult();
            var loginTimestampStr = _jsRuntime.InvokeAsync<string>("localStorage.getItem", "loginTimestamp").GetAwaiter().GetResult();
            
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
            {
                // Verificar expiración de 30 minutos
                if (!string.IsNullOrEmpty(loginTimestampStr) && long.TryParse(loginTimestampStr, out var loginTimestamp))
                {
                    var loginTime = DateTimeOffset.FromUnixTimeMilliseconds(loginTimestamp);
                    var now = DateTimeOffset.UtcNow;
                    var elapsed = now - loginTime;
                    
                    if (elapsed.TotalMinutes > 30)
                    {
                        ClearStorage();
                        return false;
                    }
                }
                else
                {
                    ClearStorage();
                    return false;
                }
                
                var user = System.Text.Json.JsonSerializer.Deserialize<UserInfo>(userJson);
                if (user != null && !IsTokenExpired(token))
                {
                    _currentUser = new LoginResponse { Token = token, User = user };
                    return true;
                }
            }
        }
        catch
        {
            // Ignorar errores
        }
        
        return false;
    }
    
    private bool CheckSessionExpiration()
    {
        try
        {
            var loginTimestampStr = _jsRuntime.InvokeAsync<string>("localStorage.getItem", "loginTimestamp").GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(loginTimestampStr) && long.TryParse(loginTimestampStr, out var loginTimestamp))
            {
                var loginTime = DateTimeOffset.FromUnixTimeMilliseconds(loginTimestamp);
                var now = DateTimeOffset.UtcNow;
                var elapsed = now - loginTime;
                
                if (elapsed.TotalMinutes > 30)
                {
                    _currentUser = null;
                    ClearStorage();
                    return false;
                }
            }
            else
            {
                _currentUser = null;
                ClearStorage();
                return false;
            }
        }
        catch
        {
            _currentUser = null;
            ClearStorage();
            return false;
        }
        
        if (IsTokenExpired(_currentUser?.Token ?? string.Empty))
        {
            _currentUser = null;
            ClearStorage();
            return false;
        }
        
        return true;
    }

    public string? GetToken()
    {
        return _currentUser?.Token;
    }

    public UserInfo? GetUser()
    {
        return _currentUser?.User;
    }

    public string? GetUserRole()
    {
        return _currentUser?.User?.Role;
    }
    
    public int? GetUserId()
    {
        return _currentUser?.User?.Id;
    }
    
    public string? GetUserName()
    {
        return _currentUser?.User?.Name;
    }
}

public class CustomAuthStateProvider : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
{
    private readonly IAuthService _authService;

    public CustomAuthStateProvider(IAuthService authService)
    {
        _authService = authService;
    }

    public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new System.Security.Claims.ClaimsIdentity();
        
        if (_authService.IsAuthenticated())
        {
            var user = _authService.GetUser();
            if (user != null)
            {
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name ?? ""),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role ?? ""),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString())
                };
                identity = new System.Security.Claims.ClaimsIdentity(claims, "jwt");
            }
        }
        
        var userPrincipal = new System.Security.Claims.ClaimsPrincipal(identity);
        return Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(userPrincipal));
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}

