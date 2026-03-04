namespace Shared.Knowledge;

public interface IEmbeddingClient
{
    Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken ct);
}
