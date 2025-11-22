namespace Gateway.Blazor.Models;

public class SystemConfiguration
{
    public int Id { get; set; }
    public string ConfigurationType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? MatchPattern { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
}

public class CreateSystemConfigurationRequest
{
    public string ConfigurationType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MatchPattern { get; set; }
    public int Priority { get; set; } = 0;
}

public class UpdateSystemConfigurationRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MatchPattern { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

