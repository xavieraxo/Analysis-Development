namespace Shared.Knowledge;

public sealed class PgVectorOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string Schema { get; init; } = "public";
    public string TableName { get; init; } = "knowledge_chunks";
    public int EmbeddingDimensions { get; init; } = 768;
    public int MaxContextChars { get; init; } = 4000;
    public int IvfLists { get; init; } = 100;
}
