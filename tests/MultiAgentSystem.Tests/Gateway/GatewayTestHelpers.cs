using System.Net;
using System.Net.Http.Json;

namespace MultiAgentSystem.Tests.Gateway;

internal static class GatewayTestHelpers
{
    public static async Task<int> CreateProjectAsync(HttpClient client, string? name = null, string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            Name = name ?? $"Proyecto {Guid.NewGuid():N}",
            Description = description ?? "Proyecto de prueba para DevFlow"
        });

        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.Created)
            throw new InvalidOperationException($"No se pudo crear proyecto de prueba. Status: {response.StatusCode}. Body: {body}");

        var project = await response.Content.ReadFromJsonAsync<TestProjectDto>();
        if (project is null)
            throw new InvalidOperationException("La respuesta de creación de proyecto fue nula.");

        return project.Id;
    }

    private sealed class TestProjectDto
    {
        public int Id { get; set; }
    }
}
