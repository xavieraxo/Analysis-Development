namespace Shared.Knowledge;

public sealed record KnowledgeChunk(string Content, string? Source = null, string? MetadataJson = null);
