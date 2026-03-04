using Npgsql;
using Pgvector;

namespace Shared.Knowledge;

internal sealed class PgVectorDatabase : IAsyncDisposable
{
    private readonly PgVectorOptions _options;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public PgVectorDatabase(PgVectorOptions options)
    {
        _options = options;
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new ArgumentException("ConnectionString es requerido para PgVector.", nameof(options));
        }

        if (options.EmbeddingDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.EmbeddingDimensions), "EmbeddingDimensions debe ser > 0.");
        }

        var builder = new NpgsqlDataSourceBuilder(options.ConnectionString);
        builder.UseVector();
        DataSource = builder.Build();
    }

    public NpgsqlDataSource DataSource { get; }

    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized)
            {
                return;
            }

            var table = PgVectorSchema.QualifiedTable(_options);
            var index = PgVectorSchema.QualifiedIndex(_options);

            await using var conn = await DataSource.OpenConnectionAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                CREATE EXTENSION IF NOT EXISTS vector;
                CREATE SCHEMA IF NOT EXISTS "{_options.Schema}";
                CREATE TABLE IF NOT EXISTS {table} (
                    id BIGSERIAL PRIMARY KEY,
                    content TEXT NOT NULL,
                    source TEXT NULL,
                    metadata JSONB NULL,
                    embedding VECTOR({_options.EmbeddingDimensions}) NOT NULL,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
                );
                CREATE INDEX IF NOT EXISTS {index}
                ON {table}
                USING ivfflat (embedding vector_cosine_ops)
                WITH (lists = {_options.IvfLists});
                """;
            await cmd.ExecuteNonQueryAsync(ct);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        return DataSource.DisposeAsync();
    }
}
