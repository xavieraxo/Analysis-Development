using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.IO;
using Data;
using Data.Models;
using Gateway.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;

namespace Gateway.Api.Services;

public class BehaviorService : IBehaviorService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<BehaviorService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public BehaviorService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<BehaviorService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<BehaviorDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var behaviors = await db.Behaviors.AsNoTracking().ToListAsync(ct);
        return behaviors.Select(MapToDto).ToList();
    }

    public async Task<BehaviorDto> GetByRoleAsync(AgentRole role, CancellationToken ct = default, bool allowFallback = true)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Behaviors.AsNoTracking().FirstOrDefaultAsync(x => x.AgentRole == role, ct);
        if (entity != null)
        {
            return MapToDto(entity);
        }

        if (!allowFallback)
        {
            throw new KeyNotFoundException($"No existe configuración para el agente {role}.");
        }

        var fallback = await LoadFallbackAsync(role, ct);
        return fallback;
    }

    public async Task<BehaviorDto> UpsertAsync(BehaviorUpsertRequest request, CancellationToken ct = default)
    {
        if (request.Prompt.Length < 1500)
        {
            throw new ValidationException("El prompt debe tener al menos 1500 caracteres.");
        }

        if (!Enum.IsDefined(typeof(AgentRole), request.AgentRole))
        {
            throw new ValidationException("AgentRole no es válido.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.Behaviors.FirstOrDefaultAsync(x => x.AgentRole == request.AgentRole, ct);
        var instructionsJson = JsonSerializer.Serialize(request.Instructions ?? new List<BehaviorInstruction>(), _jsonOptions);

        if (existing == null)
        {
            var entity = new Behavior
            {
                AgentRole = request.AgentRole,
                Alias = request.Alias,
                Prompt = request.Prompt,
                InstructionsJson = instructionsJson
            };

            db.Behaviors.Add(entity);
        }
        else
        {
            existing.Alias = request.Alias;
            existing.Prompt = request.Prompt;
            existing.InstructionsJson = instructionsJson;
            db.Behaviors.Update(existing);
        }

        await db.SaveChangesAsync(ct);
        return MapToDto(existing ?? await db.Behaviors.FirstAsync(x => x.AgentRole == request.AgentRole, ct));
    }

    public async Task<BehaviorProfile> GetBehaviorAsync(AgentRole role, CancellationToken ct = default)
    {
        var dto = await GetByRoleAsync(role, ct);
        return new BehaviorProfile
        {
            Role = dto.AgentRole,
            Alias = dto.Alias,
            Prompt = dto.Prompt,
            Instructions = dto.Instructions,
            FromFallback = dto.FromFallback
        };
    }

    private BehaviorDto MapToDto(Behavior entity)
    {
        List<BehaviorInstruction> instructions;
        try
        {
            instructions = JsonSerializer.Deserialize<List<BehaviorInstruction>>(entity.InstructionsJson, _jsonOptions) ?? new List<BehaviorInstruction>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo deserializar InstructionsJson para {Role}", entity.AgentRole);
            instructions = new List<BehaviorInstruction>();
        }

        return new BehaviorDto
        {
            AgentRole = entity.AgentRole,
            Alias = entity.Alias,
            Prompt = entity.Prompt,
            Instructions = instructions,
            FromFallback = false
        };
    }

    private async Task<BehaviorDto> LoadFallbackAsync(AgentRole role, CancellationToken ct)
    {
        var (fallbackPrompt, alias) = await Task.Run(() => LoadPromptFromFile(role), ct);
        return new BehaviorDto
        {
            AgentRole = role,
            Alias = alias,
            Prompt = fallbackPrompt,
            Instructions = new List<BehaviorInstruction>(),
            FromFallback = true
        };
    }

    private static (string Prompt, string Alias) LoadPromptFromFile(AgentRole role)
    {
        var defaults = GetDefaults(role);
        var fileName = defaults.fileName;
        var defaultPrompt = defaults.defaultPrompt;
        var alias = defaults.alias;

        var locations = GetProbePaths(fileName);
        foreach (var path in locations)
        {
            if (File.Exists(path))
            {
                try
                {
                    return (File.ReadAllText(path), alias);
                }
                catch
                {
                    // Ignorar y continuar con el siguiente path
                }
            }
        }

        return (defaultPrompt, alias);
    }

    private static (string fileName, string defaultPrompt, string alias) GetDefaults(AgentRole role) => role switch
    {
        AgentRole.Dev => ("promptAgenteDev.md", "Eres un desarrollador .NET. Propón diseño técnico y código mínimo viable, con pasos de build y pruebas.", "Dev Agent"),
        AgentRole.PM => ("promptAgentePM.md", "Eres un Project Manager. Coordina, resume, define próximos pasos y valida criterios de aceptación.", "PM Agent"),
        AgentRole.PO => ("promptAgentePO.md", "Eres un Product Owner. Defines el valor del producto, los requerimientos funcionales y representas al usuario final.", "PO Agent"),
        AgentRole.UR => ("promptUsuarioRepresentante.md", "Eres el Usuario Representante. Actúas como puente entre el cliente real y los agentes internos. Tu misión es hacer preguntas claras al cliente para obtener información validada, sin inventar ni asumir nada.", "UR Agent"),
        AgentRole.UX => ("promptAgenteUX-UI.md", "Eres un experto en UX/UI + Frontend. Diseñas experiencias de usuario, interfaces visuales y aportas visión técnica de frontend.", "UX Agent"),
        _ => ("promptAgenteDefault.md", "Comportamiento no definido, usa rol por defecto.", "Agente")
    };

    private static List<string> GetProbePaths(string fileName)
    {
        var paths = new List<string>();
        var assemblyLocation = typeof(BehaviorService).Assembly.Location;

        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDirectory))
            {
                paths.Add(Path.Combine(assemblyDirectory, fileName));
            }
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDir))
        {
            paths.Add(Path.Combine(baseDir, fileName));
        }

        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var projectDirectory = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(projectDirectory))
            {
                var projectPromptPath = Path.GetFullPath(
                    Path.Combine(projectDirectory, "..", "..", "..", fileName));
                paths.Add(projectPromptPath);
            }
        }

        return paths.Distinct().ToList();
    }
}

