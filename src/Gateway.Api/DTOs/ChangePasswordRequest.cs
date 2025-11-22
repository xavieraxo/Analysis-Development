namespace Gateway.Api.DTOs;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

