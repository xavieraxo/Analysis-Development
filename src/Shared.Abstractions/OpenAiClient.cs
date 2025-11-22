using System.Net.Http.Json;
using System.Text.Json;

namespace Shared.Abstractions;

public sealed class OpenAiClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAiClient(HttpClient http, string model)
    {
        _http = http;
        _model = model;
    }

    public async Task<string> CompleteAsync(string system, string user, CancellationToken ct)
    {
        var payload = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        using var resp = await _http.PostAsJsonAsync("/v1/chat/completions", payload, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
    }
}

