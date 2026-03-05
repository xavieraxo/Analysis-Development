namespace Gateway.Api.DTOs;

/// <summary>
/// Resultado de la ejecución de una etapa (éxito o error con código HTTP).
/// </summary>
public record ExecuteStageResult
{
    public ExecuteStageResponse? Response { get; init; }
    public int HttpStatusCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => HttpStatusCode >= 200 && HttpStatusCode < 300;

    public static ExecuteStageResult Success(ExecuteStageResponse response) =>
        new() { Response = response, HttpStatusCode = 200 };

    public static ExecuteStageResult NotFound(string message) =>
        new() { HttpStatusCode = 404, ErrorMessage = message };

    public static ExecuteStageResult Conflict(string message) =>
        new() { HttpStatusCode = 409, ErrorMessage = message };

    public static ExecuteStageResult BadRequest(string message) =>
        new() { HttpStatusCode = 400, ErrorMessage = message };
}
