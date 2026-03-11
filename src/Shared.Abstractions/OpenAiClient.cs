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

        var reqUri = _http.BaseAddress != null
            ? new Uri(_http.BaseAddress, "/v1/chat/completions")
            : new Uri("/v1/chat/completions", UriKind.Relative);
        HttpResponseMessage resp;
        try
        {
            resp = await _http.PostAsJsonAsync(reqUri, payload, ct);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }
        catch (TaskCanceledException ex)
        {
            throw new HttpRequestException(
                "La solicitud al LLM fue cancelada por tiempo de espera (timeout). El modelo puede estar cargando o tardar mucho. " +
                "Aumente OpenAI:TimeoutSeconds en configuración o use un modelo más ligero (ej. llama3.2 en lugar de uno mayor).",
                ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        using (resp)
        {
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                var friendlyMessage = BuildFriendlyErrorMessage(resp.StatusCode, _model, body);
                throw new HttpRequestException(friendlyMessage);
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (json.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var content = choices[0].GetProperty("message").GetProperty("content").GetString();
                if (!string.IsNullOrEmpty(content))
                    return content;
            }
            throw new HttpRequestException(
                "La respuesta del LLM no contiene contenido válido (choices[0].message.content vacío o ausente).");
        }
    }

    private static string BuildFriendlyErrorMessage(System.Net.HttpStatusCode statusCode, string model, string responseBody)
    {
        var (title, suggestion) = statusCode switch
        {
            System.Net.HttpStatusCode.NotFound => ("No se pudo conectar al servicio de LLM o el modelo no existe.",
                "Si usa Ollama: compruebe que esté en ejecución y que el modelo esté instalado. Ejecute 'ollama list' y use uno de los nombres en OpenAI:Model (ej. llama3.2:latest). Para instalar: 'ollama pull <nombre>'."),
            System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.BadGateway => ("El servicio de LLM no está disponible.",
                "Compruebe que Ollama (o el proveedor configurado) esté en ejecución en la URL indicada en OpenAI:BaseUrl."),
            System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.GatewayTimeout => ("La solicitud al LLM superó el tiempo de espera.",
                "Aumente OpenAI:TimeoutSeconds en configuración o use un modelo más ligero."),
            System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => ("Acceso denegado al servicio de LLM.",
                "Verifique OpenAI:Key en configuración si el proveedor requiere API key."),
            _ => ("Error al llamar al servicio de LLM.",
                $"Código: {(int)statusCode}. Revise la configuración (OpenAI:BaseUrl, OpenAI:Model) y que el servicio esté disponible.")
        };

        var serverMessage = TryGetServerErrorMessage(responseBody);
        var sb = new System.Text.StringBuilder();
        sb.Append(title);
        sb.Append(" Modelo configurado: ").Append(model).Append(".");
        if (!string.IsNullOrEmpty(serverMessage))
            sb.Append(" Servidor: \"").Append(serverMessage).Append("\".");
        sb.Append(' ').Append(suggestion);
        return sb.ToString();
    }

    private static string? TryGetServerErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;
        try
        {
            var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch { /* ignorar si no es JSON o tiene otra estructura */ }
        return null;
    }
}

