using Data.Models;

namespace Gateway.Api.DTOs;

public record LoginResponse(string Token, UserDto User);

public record UserDto(
    int Id,
    string Email,
    string Name,
    UserRole Role,
    bool IsActive,
    DateTime createdAt,
    DateTime? lastLoginAt);

