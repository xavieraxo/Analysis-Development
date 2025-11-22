using Data.Models;

namespace Gateway.Api.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string Name,
    UserRole Role);

