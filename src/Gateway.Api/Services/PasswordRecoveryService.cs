using System.Security.Cryptography;
using Data;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Services;

public class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PasswordRecoveryService> _logger;
    private readonly IEmailSender _emailSender;
    private readonly bool _useIdentity;
    private readonly UserManager<ApplicationUser>? _userManager;

    public PasswordRecoveryService(
        ApplicationDbContext db,
        ILogger<PasswordRecoveryService> logger,
        IEmailSender emailSender,
        IConfiguration config,
        IServiceProvider sp)
    {
        _db = db;
        _logger = logger;
        _emailSender = emailSender;
        _useIdentity = config.GetValue<bool>("UseIdentityAuth", false);
        if (_useIdentity)
        {
            _userManager = sp.GetService<UserManager<ApplicationUser>>();
        }
    }

    public async Task StartRecoveryAsync(string email, CancellationToken ct = default)
    {
        var emailNorm = email.Trim().ToLowerInvariant();
        var userExists = _useIdentity
            ? await _db.ApplicationUsers.AnyAsync(u => u.Email.ToLower() == emailNorm && u.IsActive, ct)
            : await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm && u.IsActive, ct);

        if (!userExists)
        {
            // Respuesta genérica; no revelar existencia
            _logger.LogInformation("Solicitud de recovery para email no encontrado (respuesta genérica).");
            return;
        }

        // invalidar códigos previos
        var previous = _db.PasswordRecoveryTokens.Where(x => x.Email == emailNorm && !x.Used);
        _db.PasswordRecoveryTokens.RemoveRange(previous);

        var code = GenerateCode();
        var token = new PasswordRecoveryToken
        {
            Email = emailNorm,
            Code = code,
            Expiration = DateTimeOffset.UtcNow.AddMinutes(10),
            Used = false
        };
        _db.PasswordRecoveryTokens.Add(token);
        await _db.SaveChangesAsync(ct);

        await _emailSender.SendAsync(email, "Código para recuperación de contraseña", $"Tu código es: {code}", ct);
    }

    public async Task<bool> VerifyCodeAsync(string email, string code, CancellationToken ct = default)
    {
        var emailNorm = email.Trim().ToLowerInvariant();
        var token = await _db.PasswordRecoveryTokens
            .Where(t => t.Email == emailNorm && t.Code == code && !t.Used && t.Expiration > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return token != null;
    }

    public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct = default)
    {
        var emailNorm = email.Trim().ToLowerInvariant();
        var token = await _db.PasswordRecoveryTokens
            .Where(t => t.Email == emailNorm && t.Code == code && !t.Used && t.Expiration > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (token == null)
        {
            return false;
        }

        if (_useIdentity)
        {
            if (_userManager == null) return false;
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm && u.IsActive, ct);
            if (user == null) return false;

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Reset password Identity falló: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
        }
        else
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm && u.IsActive, ct);
            if (user == null) return false;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.Users.Update(user);
            await _db.SaveChangesAsync(ct);
        }

        token.Used = true;
        _db.PasswordRecoveryTokens.Update(token);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> buffer = stackalloc char[6];
        using var rng = RandomNumberGenerator.Create();
        byte[] data = new byte[6];
        rng.GetBytes(data);
        for (int i = 0; i < 6; i++)
        {
            buffer[i] = chars[data[i] % chars.Length];
        }
        return new string(buffer);
    }
}

