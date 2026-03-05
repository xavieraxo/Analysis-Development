using Gateway.Api.DTOs;

namespace Gateway.Api.Services;

public interface IPasswordRecoveryService
{
    Task StartRecoveryAsync(string email, CancellationToken ct = default);
    Task<bool> VerifyCodeAsync(string email, string code, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct = default);
}

