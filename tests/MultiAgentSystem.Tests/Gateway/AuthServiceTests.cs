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

    public void Dispose()
    {
        _context?.Dispose();
    }
}

