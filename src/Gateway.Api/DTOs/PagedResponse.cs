namespace Gateway.Api.DTOs;

/// <summary>
/// Respuesta paginada genérica.
/// </summary>
/// <typeparam name="T">Tipo de los ítems.</typeparam>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}
