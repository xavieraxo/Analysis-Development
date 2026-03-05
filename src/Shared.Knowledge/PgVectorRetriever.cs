using System.Text;
using Pgvector;

namespace Shared.Knowledge;

public sealed class PgVectorRetriever : IRetriever, IAsyncDisposable
{
    private readonly PgVectorOptions _options;
    private readonly IEmbeddingClient _embeddings;
    private readonly PgVectorDatabase _db;

    public PgVectorRetriever(PgVectorOptions options, IEmbeddingClient embeddings)
    {
        _options = options;
        _embeddings = embeddings;
        _db = new PgVectorDatabase(options);
    }

    public async Task<string> GetContextAsync(string query, int k, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || k <= 0)
        {
            return string.Empty;
        }

        await _db.EnsureSchemaAsync(ct);

        var embedding = await _embeddings.EmbedAsync(query, ct);
        if (embedding.Count == 0)
        {
            return string.Empty;
        }

        var table = PgVectorSchema.QualifiedTable(_options);
        var sb = new StringBuilder();

        await using var conn = await _db.DataSource.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT content, source
            FROM {table}
            ORDER BY embedding <=> @query
            LIMIT @k;
            """;
        cmd.Parameters.AddWithValue("query", new Vector(embedding.ToArray()));
        cmd.Parameters.AddWithValue("k", k);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var content = reader.GetString(0);
            var source = reader.IsDBNull(1) ? null : reader.GetString(1);

            if (sb.Length > 0)
            {
                sb.AppendLine().AppendLine("---").AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(source))
            {
                sb.Append("Fuente: ").AppendLine(source);
            }

            sb.AppendLine(content);

            if (_options.MaxContextChars > 0 && sb.Length >= _options.MaxContextChars)
            {
                break;
            }
        }

        var result = sb.ToString();
        if (_options.MaxContextChars > 0 && result.Length > _options.MaxContextChars)
        {
            result = result.Substring(0, _options.MaxContextChars);
        }

        return result.Trim();
    }

    public ValueTask DisposeAsync()
    {
        return _db.DisposeAsync();
    }
}
