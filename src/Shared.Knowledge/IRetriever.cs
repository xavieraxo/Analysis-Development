namespace Shared.Knowledge;

public interface IRetriever
{
    Task<string> GetContextAsync(string query, int k, CancellationToken ct);
}

