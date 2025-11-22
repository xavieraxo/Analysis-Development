namespace Gateway.Api.DTOs;

public class SystemConfigurationDto
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
    public DateTime UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
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
    public string? Value { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public string? MatchPattern { get; set; }
    public int? Priority { get; set; }
}

