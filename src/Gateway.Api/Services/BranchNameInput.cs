namespace Gateway.Api.Services;

/// <summary>
/// Prefijo de rama según tipo de cambio.
/// </summary>
public enum BranchPrefix
{
    Feature,
    Fix,
    Chore
}

/// <summary>
/// Input para generar un nombre de rama.
/// </summary>
public record BranchNameInput
{
    public string Area { get; init; } = string.Empty;
    public string StoryId { get; init; } = string.Empty;
    public string TaskId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public BranchPrefix Prefix { get; init; } = BranchPrefix.Feature;
}
