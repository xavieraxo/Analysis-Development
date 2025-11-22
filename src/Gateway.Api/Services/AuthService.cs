using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Data.Models;
using Gateway.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UserDto?> RegisterAsync(RegisterRequest request);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> CreateUserAsync(CreateUserRequest request, int createdByUserId);
    Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
}

public class AuthService : IAuthService
{
    private readonly Data.ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        Data.ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var superUserHash = _configuration["SuperUser:Hash"] ?? string.Empty;
        
        // Verificar si el email es un hash de superusuario (no es un email válido)
        bool isEmailValid = IsValidEmail(request.Email);
        var emailTrimmed = request.Email?.Trim() ?? string.Empty;
        bool isSuperUserHash = !isEmailValid && !string.IsNullOrEmpty(emailTrimmed) && emailTrimmed.Equals(superUserHash, StringComparison.OrdinalIgnoreCase);
        
        _logger.LogInformation($"Login attempt - Email: {request.Email}, IsEmailValid: {isEmailValid}, IsSuperUserHash: {isSuperUserHash}, ExpectedHash: {superUserHash}");

        if (isSuperUserHash)
        {
            // Crear o obtener usuario superusuario
            var superUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == UserRole.SuperUsuario);

            if (superUser == null)
            {
                superUser = new User
                {
                    Email = "superuser@system.local",
                    Name = "Super Usuario",
                    Role = UserRole.SuperUsuario,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("superuser"),
                    IsActive = true
                };
                _context.Users.Add(superUser);
                await _context.SaveChangesAsync();
            }

            superUser.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(superUser);
            return new LoginResponse(token, MapToDto(superUser));
        }

        // Login normal - solo si el email es válido
        if (!isEmailValid)
        {
            _logger.LogWarning($"Email inválido: {request.Email}");
            return null;
        }

        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized && u.IsActive);

        _logger.LogInformation($"Intento de login - Email: {request.Email} (normalizado: {emailNormalized}), Usuario encontrado: {user != null}");

        if (user == null)
        {
            _logger.LogWarning($"Usuario no encontrado o inactivo: {request.Email}");
            return null;
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        _logger.LogInformation($"Validación de contraseña para {request.Email}: {passwordValid}");

        if (!passwordValid)
        {
            _logger.LogWarning($"Contraseña incorrecta para usuario: {request.Email}");
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Login exitoso para usuario: {user.Email}, Rol: {user.Role}");
        var jwtToken = GenerateJwtToken(user);
        return new LoginResponse(jwtToken, MapToDto(user));
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserDto?> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            return null; // Email ya existe
        }

        var user = new User
        {
            Email = normalizedEmail, // Normalizar email al registrar
            Name = request.Name.Trim(),
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return false; // No revelar si el email existe o no
        }

        // Generar token de reset
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken = token;
        user.PasswordResetExpires = DateTime.UtcNow.AddHours(1);

        await _context.SaveChangesAsync();

        // Aquí deberías enviar un email con el token
        // Por ahora solo lo logueamos
        _logger.LogInformation($"Password reset token for {email}: {token}");

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token &&
                                     u.PasswordResetExpires > DateTime.UtcNow);

        if (user == null)
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpires = null;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserRequest request, int createdByUserId)
    {
        var creator = await _context.Users.FindAsync(createdByUserId);
        if (creator == null)
        {
            return null;
        }

        // Verificar permisos según rol del creador
        // SuperUsuario, Admin, Usuario Final y Empresa pueden crear usuarios (con restricciones)
        if (creator.Role != UserRole.SuperUsuario && 
            creator.Role != UserRole.Admin &&
            creator.Role != UserRole.Final &&
            creator.Role != UserRole.Empresa)
        {
            return null;
        }

        // Normalizar email
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        
        // Verificar si el email ya existe (normalizado)
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            _logger.LogWarning($"Intento de crear usuario con email duplicado: {request.Email}");
            return null; // Email ya existe
        }

        // Validar qué tipos de usuarios puede crear según el rol
        // SuperUsuario y Admin pueden crear todos los tipos (incluyendo Admin)
        // Usuario Final y Empresa solo pueden crear Usuario Final y Empresa
        if (creator.Role == UserRole.Final || creator.Role == UserRole.Empresa)
        {
            // Usuario Final y Empresa solo pueden crear Usuario Final o Empresa
            if (request.Role != UserRole.Final && request.Role != UserRole.Empresa)
            {
                return null; // No tiene permiso para crear este tipo de usuario
            }
        }
        else if (creator.Role == UserRole.Admin)
        {
            // Admin puede crear todos los tipos, incluyendo Admin
            // No hay restricciones adicionales
        }
        // SuperUsuario puede crear todos los tipos (sin restricciones)

        var user = new User
        {
            Email = normalizedEmail, // Normalizar email al crear
            Name = request.Name.Trim(),
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = isActive;
        await _context.SaveChangesAsync();
        return true;
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-min-32-characters-long-for-security"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "MultiAgentSystem",
            audience: _configuration["Jwt:Audience"] ?? "MultiAgentSystem",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30), // Token expira en 30 minutos
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            user.Id, 
            user.Email, 
            user.Name, 
            user.Role, 
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt);
    }
}

