using Gateway.Api.DTOs;

namespace Gateway.Api.Services;

/// <summary>
/// Servicio para operaciones de DevFlow.
/// </summary>
public interface IDevFlowService
{
    /// <summary>
    /// Crea un DevFlow Run con estado inicial Created.
    /// </summary>
    /// <param name="request">Datos del run.</param>
    /// <param name="createdByUserId">ID del usuario Identity que crea el run.</param>
    /// <returns>El run creado o null si ProjectId no existe.</returns>
    Task<DevFlowRunResponse?> CreateRunAsync(CreateDevFlowRunRequest request, int createdByUserId);
}
