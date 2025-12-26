using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Data.Models;
using Gateway.Api.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Api.Services;

/// <summary>
/// Implementación de AuthService usando ASP.NET Core Identity
/// </summary>
public class AuthServiceIdentity : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthServiceIdentity> _logger;

    public AuthServiceIdentity(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthServiceIdentity> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // Validación de entrada - Email siempre requerido
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            _logger.LogWarning("Intento de login sin email");
            return null;
        }

        var superUserHash = _configuration["SuperUser:Hash"] ?? string.Empty;
        var emailTrimmed = request.Email.Trim();
        
        // Verificar si es superusuario con hash (no requiere contraseña)
        if (!string.IsNullOrEmpty(superUserHash) && 
            emailTrimmed.Equals(superUserHash, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Intento de login de SuperUsuario detectado con hash");
            return await HandleSuperUserLoginAsync(request.Password);
        }

        // Login normal requiere contraseña
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Intento de login sin contraseña");
            return null;
        }

        // Login normal con Identity
        var user = await _userManager.FindByEmailAsync(emailTrimmed);
        
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning($"Usuario no encontrado o inactivo: {emailTrimmed}");
            return null;
        }

        // Usar SignInManager para validar contraseña (incluye bloqueo automático)
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation($"Login exitoso con Identity: {user.Email}");
            var token = GenerateJwtToken(user);
            return new LoginResponse(token, MapToDto(user));
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning($"Cuenta bloqueada: {user.Email}");
            return null;
        }

        if (result.RequiresTwoFactor)
        {
            _logger.LogInformation($"Se requiere 2FA para: {user.Email}");
            return null;
        }

        _logger.LogWarning($"Contraseña incorrecta para: {user.Email}");
        return null;
    }

    private async Task<LoginResponse?> HandleSuperUserLoginAsync(string password)
    {
        var superUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Role == UserRole.SuperUsuario);

        if (superUser == null)
        {
            // Crear superusuario si no existe con contraseña por defecto
            superUser = new ApplicationUser
            {
                UserName = "superuser@system.local",
                Email = "superuser@system.local",
                Name = "Super Usuario",
                Role = UserRole.SuperUsuario,
                IsActive = true,
                EmailConfirmed = true
            };

            // Usar contraseña por defecto si no se proporciona
            var defaultPassword = string.IsNullOrEmpty(password) ? "SuperUser123@" : password;
            var createResult = await _userManager.CreateAsync(superUser, defaultPassword);
            if (!createResult.Succeeded)
            {
                _logger.LogError($"Error al crear superusuario: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                return null;
            }
            
            _logger.LogInformation("SuperUsuario creado exitosamente con Identity");
        }

        // SuperUsuario se autentica solo con el hash, sin validar contraseña
        superUser.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(superUser);

        _logger.LogInformation($"Login exitoso de SuperUsuario con hash");
        var token = GenerateJwtToken(superUser);
        return new LoginResponse(token, MapToDto(superUser));
    }

    public async Task<UserDto?> RegisterAsync(RegisterRequest request)
    {
        // Validar email único
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning($"Intento de registro con email duplicado: {request.Email}");
            return null;
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name,
            Role = request.Role,
            IsActive = true,
            EmailConfirmed = false // En producción, requerir confirmación
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation($"Usuario registrado exitosamente con Identity: {user.Email}");
            return MapToDto(user);
        }

        _logger.LogError($"Error al registrar usuario: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return null;
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive)
        {
            // No revelar si el usuario existe
            return true;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // Aquí deberías enviar un email con el token
        _logger.LogInformation($"Token de reset de contraseña generado para: {email}");
        _logger.LogInformation($"Token: {token}");

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        // Este método necesita el email o userId adicional
        // Por ahora retorna false, necesita implementación completa
        _logger.LogWarning("ResetPasswordAsync con Identity necesita userId/email adicional");
        return false;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        
        if (result.Succeeded)
        {
            _logger.LogInformation($"Contraseña cambiada exitosamente con Identity: {user.Email}");
            return true;
        }

        _logger.LogWarning($"Error al cambiar contraseña: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return false;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserRequest request, int createdByUserId)
    {
        var creator = await _userManager.FindByIdAsync(createdByUserId.ToString());
        if (creator == null)
        {
            return null;
        }

        // Validar permisos según rol del creador
        if (creator.Role != UserRole.SuperUsuario && 
            creator.Role != UserRole.Admin &&
            creator.Role != UserRole.Final &&
            creator.Role != UserRole.Empresa)
        {
            return null;
        }

        // Verificar email único
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning($"Intento de crear usuario con email duplicado: {request.Email}");
            return null;
        }

        // Validar qué tipos de usuarios puede crear según el rol
        if (creator.Role == UserRole.Final || creator.Role == UserRole.Empresa)
        {
            if (request.Role != UserRole.Final && request.Role != UserRole.Empresa)
            {
                return null;
            }
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name,
            Role = request.Role,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation($"Usuario creado exitosamente con Identity: {user.Email}");
            return MapToDto(user);
        }

        _logger.LogError($"Error al crear usuario: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return null;
    }

    public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    // Métodos privados
    private string GenerateJwtToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-min-32-characters-long-for-security"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "MultiAgentSystem",
            audience: _configuration["Jwt:Audience"] ?? "MultiAgentSystem",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToDto(ApplicationUser user)
    {
        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.Name,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt);
    }
}

