using System.Net.Http.Json;
using System.Text.Json;

namespace Shared.Knowledge;

public sealed class OpenAiEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAiEmbeddingClient(HttpClient http, string model)
    {
        _http = http;
        _model = model;
    }

    public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<float>();
        }

        var payload = new
        {
            model = _model,
            input = text
        };

        using var resp = await _http.PostAsJsonAsync("/v1/embeddings", payload, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var embeddingJson = json.GetProperty("data")[0].GetProperty("embedding");
        var vector = new float[embeddingJson.GetArrayLength()];
        var i = 0;
        foreach (var value in embeddingJson.EnumerateArray())
        {
            vector[i++] = (float)value.GetDouble();
        }

        return vector;
    }
}
