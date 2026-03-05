namespace Gateway.Api.Services;

/// <summary>
/// Genera nombres de rama consistentes con la convención feature/area/slug.
/// </summary>
public interface IBranchNameGenerator
{
    /// <summary>
    /// Genera un nombre de rama a partir del input.
    /// </summary>
    /// <param name="input">Área, tarea, título y prefijo.</param>
    /// <returns>Nombre de rama, p.ej. feature/autodev/5.2.1-create-run-endpoint.</returns>
    /// <exception cref="ArgumentException">Si Area está vacío.</exception>
    string Generate(BranchNameInput input);
}
