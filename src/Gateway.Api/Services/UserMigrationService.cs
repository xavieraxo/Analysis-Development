using Data;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Services;

public interface IUserMigrationService
{
    Task<MigrationResult> MigrateUsersToIdentityAsync();
}

public class UserMigrationService : IUserMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserMigrationService> _logger;

    public UserMigrationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<UserMigrationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateUsersToIdentityAsync()
    {
        var result = new MigrationResult();
        var legacyUsers = await _context.Users.ToListAsync();

        _logger.LogInformation($"Iniciando migración de {legacyUsers.Count} usuarios a Identity...");

        foreach (var legacyUser in legacyUsers)
        {
            try
            {
                // Verificar si ya existe
                var existing = await _userManager.FindByEmailAsync(legacyUser.Email);
                if (existing != null)
                {
                    result.Skipped++;
                    _logger.LogInformation($"Usuario {legacyUser.Email} ya existe en IdentityUsers, omitiendo...");
                    continue;
                }

                var appUser = new ApplicationUser
                {
                    UserName = legacyUser.Email,
                    Email = legacyUser.Email,
                    Name = legacyUser.Name,
                    Role = legacyUser.Role,
                    IsActive = legacyUser.IsActive,
                    CreatedAt = legacyUser.CreatedAt,
                    LastLoginAt = legacyUser.LastLoginAt,
                    EmailConfirmed = true, // Asumir que emails están confirmados
                    LegacyUserId = legacyUser.Id // Guardar referencia al ID antiguo
                };

                // IMPORTANTE: No podemos migrar hashes de BCrypt a Identity (usan diferentes algoritmos)
                // Opción: Setear contraseña temporal que el usuario debe cambiar
                var tempPassword = $"Temp{legacyUser.Id}!Abc";
                var createResult = await _userManager.CreateAsync(appUser, tempPassword);

                if (createResult.Succeeded)
                {
                    result.Migrated++;
                    result.TempPasswords.Add($"{legacyUser.Email} -> {tempPassword}");
                    _logger.LogInformation($"Usuario {legacyUser.Email} migrado exitosamente. Password temporal: {tempPassword}");
                }
                else
                {
                    result.Failed++;
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    result.Errors.Add($"{legacyUser.Email}: {errors}");
                    _logger.LogError($"Error al migrar usuario {legacyUser.Email}: {errors}");
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"{legacyUser.Email}: {ex.Message}");
                _logger.LogError(ex, $"Excepción al migrar usuario {legacyUser.Email}");
            }
        }

        _logger.LogInformation($"Migración completada. Migrados: {result.Migrated}, Omitidos: {result.Skipped}, Fallidos: {result.Failed}");

        return result;
    }
}

public class MigrationResult
{
    public int Migrated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> TempPasswords { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

