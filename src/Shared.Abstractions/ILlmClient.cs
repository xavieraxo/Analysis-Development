namespace Shared.Abstractions;

public interface ILlmClient
{
    Task<string> CompleteAsync(string system, string user, CancellationToken ct);
}

