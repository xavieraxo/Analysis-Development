using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Data.Models;
using Gateway.Api.DTOs;
using Gateway.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MultiAgentSystem.Tests.Gateway;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Configurar base de datos en memoria para tests
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Configurar IConfiguration con valores de test
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "SuperUser:Hash", "A4F1C72E99B3842F7D1A5C0083F6D2B1" },
            { "Jwt:Key", "your-secret-key-min-32-characters-long-for-security-purposes-change-in-production" },
            { "Jwt:Issuer", "MultiAgentSystem" },
            { "Jwt:Audience", "MultiAgentSystem" }
        });
        _configuration = configurationBuilder.Build();

        _loggerMock = new Mock<ILogger<AuthService>>();
        _authService = new AuthService(_context, _configuration, _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidSuperUserHash_ShouldReturnLoginResponse()
    {
        // Arrange
        var request = new LoginRequest("A4F1C72E99B3842F7D1A5C0083F6D2B1", "");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal("SuperUsuario", result.User.Role.ToString());
        Assert.Equal("Super Usuario", result.User.Name);
    }

    [Fact]
    public async Task LoginAsync_WithValidSuperUserHash_ShouldCreateSuperUserIfNotExists()
    {
        // Arrange
        var request = new LoginRequest("A4F1C72E99B3842F7D1A5C0083F6D2B1", "");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        var superUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.SuperUsuario);
        Assert.NotNull(superUser);
        Assert.Equal("superuser@system.local", superUser.Email);
        Assert.Equal("Super Usuario", superUser.Name);
    }

    [Fact]
    public async Task LoginAsync_WithValidSuperUserHash_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var request = new LoginRequest("A4F1C72E99B3842F7D1A5C0083F6D2B1", "");
        await _authService.LoginAsync(request); // Primera vez para crear el usuario

        // Act
        var beforeLogin = DateTime.UtcNow;
        var result = await _authService.LoginAsync(request);
        var afterLogin = DateTime.UtcNow;

        // Assert
        var superUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.SuperUsuario);
        Assert.NotNull(superUser);
        Assert.NotNull(superUser.LastLoginAt);
        Assert.True(superUser.LastLoginAt >= beforeLogin && superUser.LastLoginAt <= afterLogin);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidHash_ShouldReturnNull()
    {
        // Arrange
        var request = new LoginRequest("INVALID_HASH_12345", "");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithValidEmailAndPassword_ShouldReturnLoginResponse()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            Role = UserRole.Final,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("test@example.com", "password123");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal("test@example.com", result.User.Email);
        Assert.Equal("Test User", result.User.Name);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            Role = UserRole.Final,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("test@example.com", "wrongpassword");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ShouldReturnNull()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "password123");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ShouldReturnNull()
    {
        // Arrange
        var user = new User
        {
            Email = "inactive@example.com",
            Name = "Inactive User",
            Role = UserRole.Final,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("inactive@example.com", "password123");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithHashCaseInsensitive_ShouldWork()
    {
        // Arrange
        var request = new LoginRequest("a4f1c72e99b3842f7d1a5c0083f6d2b1", ""); // lowercase

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.Equal("SuperUsuario", result.User.Role.ToString());
    }

    [Fact]
    public async Task LoginAsync_WithHashWithSpaces_ShouldWork()
    {
        // Arrange
        var request = new LoginRequest("  A4F1C72E99B3842F7D1A5C0083F6D2B1  ", ""); // con espacios

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.Equal("SuperUsuario", result.User.Role.ToString());
    }

    [Fact]
    public async Task LoginAsync_WithValidEmailButInvalidFormat_ShouldReturnNull()
    {
        // Arrange
        var request = new LoginRequest("not-an-email", "password");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithValidEmailButNotHash_ShouldReturnNull()
    {
        // Arrange
        var request = new LoginRequest("some-other-hash-value", "");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    // ========== TESTS DE REGISTRO ==========

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserWithFinalRole()
    {
        // Arrange
        var request = new RegisterRequest(
            "newuser@example.com",
            "Password123@",
            "New User",
            UserRole.Final
        );

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser@example.com", result.Email);
        Assert.Equal("New User", result.Name);
        Assert.Equal(UserRole.Final, result.Role);
        Assert.True(result.IsActive);

        // Verificar que se guardó en la base de datos
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        Assert.NotNull(user);
        Assert.Equal("newuser@example.com", user.Email);
        Assert.Equal("New User", user.Name);
        Assert.Equal(UserRole.Final, user.Role);
        Assert.True(user.IsActive);
        
        // Verificar que la contraseña está hasheada (no es la contraseña original)
        Assert.NotEqual("Password123@", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123@", user.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldNormalizeEmail()
    {
        // Arrange
        var request = new RegisterRequest(
            "  TestUser@EXAMPLE.COM  ", // Con espacios y mayúsculas
            "Password123@",
            "Test User",
            UserRole.Final
        );

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        // El email debe estar normalizado (lowercase y sin espacios)
        Assert.Equal("testuser@example.com", result.Email);

        // Verificar en la base de datos
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "testuser@example.com");
        Assert.NotNull(user);
        Assert.Equal("testuser@example.com", user.Email);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldReturnNull()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            Name = "Existing User",
            Role = UserRole.Final,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = true
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest(
            "existing@example.com", // Email duplicado
            "Password123@",
            "New User",
            UserRole.Final
        );

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.Null(result); // Debe retornar null cuando el email ya existe

        // Verificar que no se creó un usuario duplicado
        var users = await _context.Users.Where(u => u.Email == "existing@example.com").ToListAsync();
        Assert.Single(users); // Solo debe haber un usuario
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmailCaseInsensitive_ShouldReturnNull()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            Name = "Existing User",
            Role = UserRole.Final,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = true
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest(
            "EXISTING@EXAMPLE.COM", // Email duplicado con diferente case
            "Password123@",
            "New User",
            UserRole.Final
        );

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.Null(result); // Debe retornar null porque el email ya existe (case-insensitive)

        // Verificar que no se creó un usuario duplicado
        var users = await _context.Users.Where(u => u.Email.ToLower() == "existing@example.com").ToListAsync();
        Assert.Single(users); // Solo debe haber un usuario
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        // Arrange
        var request = new RegisterRequest(
            "testhash@example.com",
            "Password123@",
            "Test User",
            UserRole.Final
        );

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);

        // Verificar que la contraseña está hasheada en la base de datos
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "testhash@example.com");
        Assert.NotNull(user);
        
        // La contraseña no debe ser la original
        Assert.NotEqual("Password123@", user.PasswordHash);
        
        // La contraseña debe poder verificarse con BCrypt
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123@", user.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var request = new RegisterRequest(
            "activeuser@example.com",
            "Password123@",
            "Active User",
            UserRole.Final
        );

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive); // El usuario debe estar activo por defecto

        // Verificar en la base de datos
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "activeuser@example.com");
        Assert.NotNull(user);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task RegisterAsync_WithDifferentRoles_ShouldCreateUserWithCorrectRole()
    {
        // Arrange - Registro de usuario Final
        var requestFinal = new RegisterRequest(
            "finaluser@example.com",
            "Password123@",
            "Final User",
            UserRole.Final
        );

        // Act
        var resultFinal = await _authService.RegisterAsync(requestFinal);

        // Assert
        Assert.NotNull(resultFinal);
        Assert.Equal(UserRole.Final, resultFinal.Role);

        // Arrange - Registro de usuario Empresa
        var requestEmpresa = new RegisterRequest(
            "empresauser@example.com",
            "Password123@",
            "Empresa User",
            UserRole.Empresa
        );

        // Act
        var resultEmpresa = await _authService.RegisterAsync(requestEmpresa);

        // Assert
        Assert.NotNull(resultEmpresa);
        Assert.Equal(UserRole.Empresa, resultEmpresa.Role);

        // Verificar en la base de datos
        var userFinal = await _context.Users.FirstOrDefaultAsync(u => u.Email == "finaluser@example.com");
        var userEmpresa = await _context.Users.FirstOrDefaultAsync(u => u.Email == "empresauser@example.com");
        
        Assert.NotNull(userFinal);
        Assert.Equal(UserRole.Final, userFinal.Role);
        
        Assert.NotNull(userEmpresa);
        Assert.Equal(UserRole.Empresa, userEmpresa.Role);
    }

    [Fact]
    public async Task RegisterAsync_ShouldTrimName()
    {
        // Arrange
        var request = new RegisterRequest(
            "trimname@example.com",
            "Password123@",
            "  Test User  ", // Con espacios
            UserRole.Final
        );

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test User", result.Name); // El nombre debe estar sin espacios

        // Verificar en la base de datos
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "trimname@example.com");
        Assert.NotNull(user);
        Assert.Equal("Test User", user.Name);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

