using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shared.Knowledge;

namespace MultiAgentSystem.Tests.Knowledge;

public class PgVectorTests
{
    [Fact]
    public void PgVectorSchema_ValidIdentifiers_NoThrow()
    {
        PgVectorSchema.ValidateIdentifier("public", "schema");
        PgVectorSchema.ValidateIdentifier("knowledge_chunks", "table");
    }

    [Fact]
    public void PgVectorSchema_InvalidIdentifiers_Throw()
    {
        Assert.Throws<ArgumentException>(() => PgVectorSchema.ValidateIdentifier("", "schema"));
        Assert.Throws<ArgumentException>(() => PgVectorSchema.ValidateIdentifier("bad-name", "table"));
        Assert.Throws<ArgumentException>(() => PgVectorSchema.ValidateIdentifier("1table", "table"));
    }

    [Fact]
    public void PgVectorSchema_BuildsQualifiedNames()
    {
        var options = new PgVectorOptions
        {
            ConnectionString = "Host=localhost;Port=5433;Database=multiagent;Username=appuser;Password=appsecret",
            Schema = "public",
            TableName = "knowledge_chunks"
        };

        var table = PgVectorSchema.QualifiedTable(options);
        var index = PgVectorSchema.QualifiedIndex(options);

        Assert.Equal("\"public\".\"knowledge_chunks\"", table);
        Assert.Equal("\"public\".\"knowledge_chunks_embedding_idx\"", index);
    }

    [Fact]
    public async Task PgVectorRetriever_EmptyQueryOrK_ReturnsEmpty()
    {
        var options = new PgVectorOptions
        {
            ConnectionString = "Host=localhost;Port=5433;Database=multiagent;Username=appuser;Password=appsecret"
        };
        var embeddings = new ThrowingEmbeddingClient();
        var retriever = new PgVectorRetriever(options, embeddings);

        var emptyQuery = await retriever.GetContextAsync("", 3, CancellationToken.None);
        var zeroK = await retriever.GetContextAsync("hola", 0, CancellationToken.None);

        Assert.Equal(string.Empty, emptyQuery);
        Assert.Equal(string.Empty, zeroK);
    }

    [Fact]
    public async Task PgVectorKnowledgeStore_EmptyList_NoThrow()
    {
        var options = new PgVectorOptions
        {
            ConnectionString = "Host=localhost;Port=5433;Database=multiagent;Username=appuser;Password=appsecret"
        };
        var embeddings = new ThrowingEmbeddingClient();
        var store = new PgVectorKnowledgeStore(options, embeddings);

        await store.AddManyAsync(new List<KnowledgeChunk>(), CancellationToken.None);
    }

    private sealed class ThrowingEmbeddingClient : IEmbeddingClient
    {
        public Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken ct)
        {
            throw new InvalidOperationException("No se esperaba llamar a embeddings en este test.");
        }
    }
}
