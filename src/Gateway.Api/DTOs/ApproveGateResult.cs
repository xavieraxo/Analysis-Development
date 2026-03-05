namespace Gateway.Api.DTOs;

/// <summary>
/// Resultado de la aprobación de un gate (éxito o error con código HTTP).
/// </summary>
public record ApproveGateResult
{
    public ApproveGateResponse? Response { get; init; }
    public int HttpStatusCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => HttpStatusCode >= 200 && HttpStatusCode < 300;

    public static ApproveGateResult Success(ApproveGateResponse response) =>
        new() { Response = response, HttpStatusCode = 200 };

    public static ApproveGateResult NotFound(string message) =>
        new() { HttpStatusCode = 404, ErrorMessage = message };

    public static ApproveGateResult BadRequest(string message) =>
        new() { HttpStatusCode = 400, ErrorMessage = message };
}
