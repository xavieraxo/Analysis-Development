namespace Shared.Knowledge;

public interface IKnowledgeStore
{
    Task AddAsync(KnowledgeChunk chunk, CancellationToken ct);
    Task AddManyAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken ct);
}
