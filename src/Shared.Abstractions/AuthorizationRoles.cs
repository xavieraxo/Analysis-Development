namespace Shared.Abstractions;

/// <summary>
/// Roles de autorización centralizados.
/// Protocolo: solo SuperUsuario puede modificar configuraciones internas del sistema.
/// </summary>
public static class AuthorizationRoles
{
    // Roles
    /// <summary>
    /// Rol que puede crear/editar/eliminar configuraciones internas (behaviors, configurations, etc.).
    /// </summary>
    public const string SuperUsuario = "SuperUsuario";

    /// <summary>
    /// Rol administrativo de aplicación (gestión global de usuarios/proyectos, sin modificar lógica interna).
    /// </summary>
    public const string AppAdmin = "AppAdmin";

    /// <summary>
    /// Rol administrativo de empresa/tenant.
    /// </summary>
    public const string TenantAdmin = "TenantAdmin";

    /// <summary>
    /// Rol operativo de empresa (operadores sobre proyectos dentro de su ámbito).
    /// </summary>
    public const string OperadorEmpresa = "OperadorEmpresa";

    /// <summary>
    /// Nombre de la policy para endpoints que solo SuperUsuario puede acceder.
    /// </summary>
    public const string SuperUserOnlyPolicy = "SuperUserOnly";

    /// <summary>
    /// Policy para endpoints accesibles por AppAdmin o SuperUsuario.
    /// </summary>
    public const string AppAdminOrSuperUserPolicy = "AppAdminOrSuperUser";

    /// <summary>
    /// Policy para validar acceso a un proyecto según ownership/asignación.
    /// </summary>
    public const string CanAccessProjectPolicy = "CanAccessProject";

    /// <summary>
    /// Policy para ejecutar etapas de DevFlow en un run.
    /// </summary>
    public const string CanExecuteDevFlowStagePolicy = "CanExecuteDevFlowStage";

    /// <summary>
    /// Policy para aprobar gates de DevFlow.
    /// </summary>
    public const string CanApproveGatePolicy = "CanApproveGate";
}
