using Data.Models;

namespace Gateway.Api.DTOs;

public record CreateUserRequest(
    string Email,
    string Password,
    string Name,
    UserRole Role);

