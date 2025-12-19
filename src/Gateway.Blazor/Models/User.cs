using System.Text.Json.Serialization;
using Gateway.Blazor.Converters;

namespace Gateway.Blazor.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    [JsonConverter(typeof(UserRoleJsonConverter))]
    public string Role { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

