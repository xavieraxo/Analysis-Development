using NpgsqlTypes;
using Pgvector;

namespace Shared.Knowledge;

public sealed class PgVectorKnowledgeStore : IKnowledgeStore, IAsyncDisposable
{
    private readonly PgVectorOptions _options;
    private readonly IEmbeddingClient _embeddings;
    private readonly PgVectorDatabase _db;

    public PgVectorKnowledgeStore(PgVectorOptions options, IEmbeddingClient embeddings)
    {
        _options = options;
        _embeddings = embeddings;
        _db = new PgVectorDatabase(options);
    }

    public async Task AddAsync(KnowledgeChunk chunk, CancellationToken ct)
    {
        await AddManyAsync(new[] { chunk }, ct);
    }

    public async Task AddManyAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken ct)
    {
        var list = chunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk.Content))
            .ToList();

        if (list.Count == 0)
        {
            return;
        }

        await _db.EnsureSchemaAsync(ct);

        var table = PgVectorSchema.QualifiedTable(_options);
        await using var conn = await _db.DataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        foreach (var chunk in list)
        {
            var embedding = await _embeddings.EmbedAsync(chunk.Content, ct);
            if (embedding.Count == 0)
            {
                continue;
            }

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $"""
                INSERT INTO {table} (content, source, metadata, embedding)
                VALUES (@content, @source, @metadata, @embedding);
                """;
            cmd.Parameters.AddWithValue("content", chunk.Content);
            cmd.Parameters.AddWithValue("source", (object?)chunk.Source ?? DBNull.Value);

            var metadataParam = cmd.Parameters.Add("metadata", NpgsqlDbType.Jsonb);
            metadataParam.Value = string.IsNullOrWhiteSpace(chunk.MetadataJson)
                ? DBNull.Value
                : chunk.MetadataJson!;

            cmd.Parameters.AddWithValue("embedding", new Vector(embedding.ToArray()));

            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        return _db.DisposeAsync();
    }
}
