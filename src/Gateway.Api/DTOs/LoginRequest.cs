namespace Gateway.Api.DTOs;

public record LoginRequest(string Email, string Password, string? SuperUserHash = null);

