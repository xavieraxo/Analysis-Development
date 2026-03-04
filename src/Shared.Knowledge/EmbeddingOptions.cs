namespace Shared.Knowledge;

public sealed class EmbeddingOptions
{
    public string BaseUrl { get; init; } = "http://localhost:11434/v1";
    public string Model { get; init; } = "nomic-embed-text";
    public string? Key { get; init; }
    public int TimeoutSeconds { get; init; } = 600;
}
