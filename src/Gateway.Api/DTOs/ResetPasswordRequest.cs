namespace Gateway.Api.DTOs;

public record ResetPasswordRequest(string Token, string NewPassword);

