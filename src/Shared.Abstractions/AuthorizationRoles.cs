namespace Shared.Abstractions;

/// <summary>
/// Roles de autorización centralizados.
/// Protocolo: solo SuperUsuario puede modificar configuraciones internas del sistema.
/// </summary>
public static class AuthorizationRoles
{
    /// <summary>
    /// Rol que puede crear/editar/eliminar configuraciones internas (behaviors, configurations, etc.).
    /// </summary>
    public const string SuperUsuario = "SuperUsuario";

    /// <summary>
    /// Nombre de la policy para endpoints que solo SuperUsuario puede acceder.
    /// </summary>
    public const string SuperUserOnlyPolicy = "SuperUserOnly";
}
