using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Agents.Dev;
using Agents.PM;
using Agents.PO;
using Agents.UX;
using Agents.UR;
using Data;
using Data.Models;
using Gateway.Api.DTOs;
using Gateway.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Abstractions;
using OrchestratorApp = Orchestrator.App.Orchestrator;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configurar Entity Framework Core con SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=multiagent.db"));

// Configurar JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-secret-key-min-32-characters-long-for-security-purposes";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MultiAgentSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MultiAgentSystem";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "SuperUsuario"));
    options.AddPolicy("AdminOrSuperUser", policy => policy.RequireRole("Admin", "SuperUsuario"));
});

// Configurar HttpClient para OpenAI
var baseUrl = builder.Configuration["OpenAI:BaseUrl"] ?? "http://localhost:11434/v1";
var model = builder.Configuration["OpenAI:Model"] ?? "llama3.2";
var timeoutSeconds = builder.Configuration.GetValue<int>("OpenAI:TimeoutSeconds", 600);

builder.Services.AddHttpClient<OpenAiClient>(c =>
{
    c.BaseAddress = new Uri(baseUrl);
    c.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    var apiKey = builder.Configuration["OpenAI:Key"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        c.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
});

builder.Services.AddSingleton<ILlmClient>(provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OpenAiClient));
    return new OpenAiClient(httpClient, model);
});

// Registrar servicios
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAgentLoggingService, AgentLoggingService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Registrar agentes
builder.Services.AddSingleton<IAgent, UrAgent>();
builder.Services.AddSingleton<IAgent, PmAgent>();
builder.Services.AddSingleton<IAgent, PoAgent>();
builder.Services.AddSingleton<IAgent, DevAgent>();
builder.Services.AddSingleton<IAgent, UxAgent>();

// Registrar Orchestrator con logging
builder.Services.AddSingleton<OrchestratorApp>(provider =>
{
    var agents = provider.GetServices<IAgent>();
    var llm = provider.GetRequiredService<ILlmClient>();
    var loggingService = provider.GetRequiredService<IAgentLoggingService>();
    return new OrchestratorApp(agents, llm, (Orchestrator.App.ILoggingCallback?)loggingService);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Multi-Agent Gateway API",
        Version = "v1",
        Description = "Orquestador de agentes (UR, PM, PO, Dev, UX) expuesto vía REST y WebSocket"
    });

    // Configurar JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS para desarrollo
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Aplicar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // En desarrollo, eliminar y recrear la base de datos si el esquema cambió
    // En producción, usar migraciones de EF Core
    if (app.Environment.IsDevelopment())
    {
        db.Database.EnsureDeleted(); // Eliminar base de datos existente
    }
    db.Database.EnsureCreated(); // Crear base de datos con el esquema actualizado
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Multi-Agent Gateway API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ========== ENDPOINTS PÚBLICOS ==========

// Login
app.MapPost("/api/auth/login", async (IAuthService authService, LoginRequest request) =>
{
    var result = await authService.LoginAsync(request);
    if (result == null)
    {
        return Results.Unauthorized();
    }
    return Results.Ok(result);
})
.WithName("Login")
.Produces<LoginResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized);

// Registro
app.MapPost("/api/auth/register", async (IAuthService authService, RegisterRequest request) =>
{
    var result = await authService.RegisterAsync(request);
    if (result == null)
    {
        return Results.BadRequest(new { message = "El email ya está registrado" });
    }
    return Results.Ok(result);
})
.WithName("Register")
.Produces<UserDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// Olvidé mi contraseña
app.MapPost("/api/auth/forgot-password", async (IAuthService authService, ForgotPasswordRequest request) =>
{
    var result = await authService.ForgotPasswordAsync(request.Email);
    return Results.Ok(new { message = "Si el email existe, se enviará un enlace de recuperación" });
})
.WithName("ForgotPassword")
.Produces(StatusCodes.Status200OK);

// Reset password
app.MapPost("/api/auth/reset-password", async (IAuthService authService, ResetPasswordRequest request) =>
{
    var result = await authService.ResetPasswordAsync(request.Token, request.NewPassword);
    if (!result)
    {
        return Results.BadRequest(new { message = "Token inválido o expirado" });
    }
    return Results.Ok(new { message = "Contraseña restablecida exitosamente" });
})
.WithName("ResetPassword")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// ========== ENDPOINTS PROTEGIDOS ==========

// Obtener usuario actual
app.MapGet("/api/auth/me", [Authorize] async (HttpContext context, IAuthService authService) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var user = await authService.GetUserByIdAsync(userId);
    return user == null ? Results.NotFound() : Results.Ok(user);
})
.WithName("GetCurrentUser")
.Produces<UserDto>(StatusCodes.Status200OK);

// Cambiar contraseña
app.MapPost("/api/auth/change-password", [Authorize] async (HttpContext context, IAuthService authService, ChangePasswordRequest request) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
    if (!result)
    {
        return Results.BadRequest(new { message = "Contraseña actual incorrecta" });
    }
    return Results.Ok(new { message = "Contraseña cambiada exitosamente" });
})
.WithName("ChangePassword")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// Proyectos del usuario
app.MapGet("/api/projects", [Authorize] async (HttpContext context, IProjectService projectService) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var projects = await projectService.GetUserProjectsAsync(userId);
    return Results.Ok(projects);
})
.WithName("GetUserProjects")
.Produces<List<ProjectDto>>(StatusCodes.Status200OK);

// Crear proyecto
app.MapPost("/api/projects", [Authorize] async (HttpContext context, IProjectService projectService, CreateProjectRequest request) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var project = await projectService.CreateProjectAsync(userId, request.Name, request.Description);
    return Results.Created($"/api/projects/{project.Id}", project);
})
.WithName("CreateProject")
.Produces<ProjectDto>(StatusCodes.Status201Created);

// Obtener proyecto por ID
app.MapGet("/api/projects/{id}", [Authorize] async (int id, HttpContext context, IProjectService projectService) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var project = await projectService.GetProjectByIdAsync(id, userId);
    return project == null ? Results.NotFound() : Results.Ok(project);
})
.WithName("GetProject")
.Produces<ProjectDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// Endpoint REST para chat (requiere autenticación y proyecto)
app.MapPost("/api/chat/run", [Authorize] async (
    OrchestratorApp orch,
    IProjectService projectService,
    IAgentLoggingService loggingService,
    IConfigurationService configurationService,
    HttpContext context,
    ChatMessage input,
    int? projectId,
    CancellationToken ct) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    // Si hay projectId, validar que pertenece al usuario
    if (projectId.HasValue)
    {
        var project = await projectService.GetProjectByIdAsync(projectId.Value, userId);
        if (project == null)
        {
            return Results.NotFound(new { message = "Proyecto no encontrado" });
        }
    }

    // Verificar si hay una respuesta automática para este mensaje
    var autoResponse = await configurationService.GetAutoResponseAsync(input.Text);
    if (!string.IsNullOrEmpty(autoResponse))
    {
        // Devolver respuesta automática sin procesar con agentes
        var autoResponseMessage = new ChatMessage(
            input.ConversationId,
            AgentRole.User, // O podríamos usar un rol especial
            autoResponse,
            DateTimeOffset.UtcNow
        );
        
        return Results.Ok(new ChatResponse(autoResponseMessage, new List<ChatMessage>()));
    }

    var flow = new[] { AgentRole.UR, AgentRole.PM, AgentRole.PO, AgentRole.UX, AgentRole.Dev, AgentRole.PM };
    var response = await orch.RunWithSummaryAsync(input.ConversationId, input, flow, ct);

    // Guardar análisis en proyecto si hay projectId
    if (projectId.HasValue)
    {
        var internalConversationsJson = JsonSerializer.Serialize(response.InternalConversations);
        await projectService.SaveProjectAnalysisAsync(
            projectId.Value,
            input.ConversationId,
            response.Summary.Text,
            internalConversationsJson);
    }

    return Results.Ok(response);
})
.WithName("RunChatFlow")
.Produces<ChatResponse>(StatusCodes.Status200OK);

// ========== ENDPOINTS DE CONFIGURACIÓN ==========

// Obtener todas las configuraciones (todos los usuarios autenticados pueden ver)
app.MapGet("/api/configurations", [Authorize] async (IConfigurationService configService) =>
{
    var configs = await configService.GetAllConfigurationsAsync();
    return Results.Ok(configs);
})
.WithName("GetAllConfigurations")
.Produces<List<SystemConfigurationDto>>(StatusCodes.Status200OK);

// Obtener configuraciones por tipo
app.MapGet("/api/configurations/{type}", [Authorize] async (string type, IConfigurationService configService) =>
{
    var configs = await configService.GetConfigurationsByTypeAsync(type);
    return Results.Ok(configs);
})
.WithName("GetConfigurationsByType")
.Produces<List<SystemConfigurationDto>>(StatusCodes.Status200OK);

// Crear configuración (todos los usuarios autenticados pueden crear)
app.MapPost("/api/configurations", [Authorize] async (
    HttpContext context,
    IConfigurationService configService,
    CreateSystemConfigurationRequest request) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var config = await configService.CreateConfigurationAsync(request, userId);
    
    if (config == null)
    {
        return Results.BadRequest(new { message = "La configuración ya existe o hubo un error al crearla" });
    }
    
    return Results.Created($"/api/configurations/{config.Id}", config);
})
.WithName("CreateConfiguration")
.Produces<SystemConfigurationDto>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

// Actualizar configuración (todos los usuarios autenticados pueden actualizar)
app.MapPut("/api/configurations/{id}", [Authorize] async (
    int id,
    HttpContext context,
    IConfigurationService configService,
    UpdateSystemConfigurationRequest request) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var config = await configService.UpdateConfigurationAsync(id, request, userId);
    
    if (config == null)
    {
        return Results.NotFound(new { message = "Configuración no encontrada" });
    }
    
    return Results.Ok(config);
})
.WithName("UpdateConfiguration")
.Produces<SystemConfigurationDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// Activar/Desactivar configuración (todos los usuarios autenticados pueden activar/desactivar)
app.MapPatch("/api/configurations/{id}/toggle", [Authorize] async (
    int id,
    HttpContext context,
    IConfigurationService configService,
    bool isActive) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await configService.ToggleConfigurationAsync(id, isActive, userId);
    
    if (!result)
    {
        return Results.NotFound(new { message = "Configuración no encontrada" });
    }
    
    return Results.Ok(new { message = "Configuración actualizada" });
})
.WithName("ToggleConfiguration")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// Eliminar configuración (solo Admin y SuperUsuario pueden eliminar)
app.MapDelete("/api/configurations/{id}", [Authorize(Policy = "AdminOrSuperUser")] async (
    int id,
    IConfigurationService configService) =>
{
    var result = await configService.DeleteConfigurationAsync(id);
    
    if (!result)
    {
        return Results.NotFound(new { message = "Configuración no encontrada" });
    }
    
    return Results.Ok(new { message = "Configuración eliminada" });
})
.WithName("DeleteConfiguration")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// ========== ENDPOINTS SOLO ADMIN/SUPERUSUARIO ==========

// Listar todos los proyectos (Admin/SuperUsuario)
app.MapGet("/api/admin/projects", [Authorize(Policy = "AdminOrSuperUser")] async (IProjectService projectService) =>
{
    var projects = await projectService.GetAllProjectsAsync();
    return Results.Ok(projects);
})
.WithName("GetAllProjects")
.Produces<List<ProjectDto>>(StatusCodes.Status200OK);

// Obtener logs de una conversación (Admin/SuperUsuario)
app.MapGet("/api/admin/logs/{conversationId}", [Authorize(Policy = "AdminOrSuperUser")] async (string conversationId, IAgentLoggingService loggingService) =>
{
    var logs = await loggingService.GetConversationLogsAsync(conversationId);
    return Results.Ok(logs);
})
.WithName("GetConversationLogs")
.Produces<List<AgentLogEntry>>(StatusCodes.Status200OK);

// Obtener todos los logs (Admin/SuperUsuario)
app.MapGet("/api/admin/logs", [Authorize(Policy = "AdminOrSuperUser")] async (IAgentLoggingService loggingService, int? limit) =>
{
    var logs = await loggingService.GetAllLogsAsync(limit);
    return Results.Ok(logs);
})
.WithName("GetAllLogs")
.Produces<List<AgentLogEntry>>(StatusCodes.Status200OK);

// ========== ENDPOINTS SOLO SUPERUSUARIO ==========

// Listar todos los usuarios (Solo SuperUsuario)
app.MapGet("/api/admin/users", [Authorize] async (HttpContext context, IAuthService authService) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var user = await authService.GetUserByIdAsync(userId);
    
    if (user == null || user.Role != Data.Models.UserRole.SuperUsuario)
    {
        return Results.Forbid();
    }

    var users = await authService.GetAllUsersAsync();
    return Results.Ok(users);
})
.WithName("GetAllUsers")
.Produces<List<UserDto>>(StatusCodes.Status200OK);

// Crear usuario (SuperUsuario, Admin, Usuario Final, Empresa - con restricciones)
app.MapPost("/api/admin/users", [Authorize] async (HttpContext context, IAuthService authService, CreateUserRequest request, ILogger<Program> logger) =>
{
    var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var roleClaim = context.User.FindFirstValue(ClaimTypes.Role);
    
    logger.LogInformation($"Token claims - UserId: {userIdClaim}, Role: {roleClaim}");
    
    if (string.IsNullOrEmpty(userIdClaim))
    {
        logger.LogWarning("Token no contiene UserId");
        return Results.Json(new { message = "Token inválido" }, statusCode: 403);
    }
    
    var userId = int.Parse(userIdClaim);
    var user = await authService.GetUserByIdAsync(userId);
    
    if (user == null)
    {
        logger.LogWarning($"Usuario no encontrado en BD: {userId}");
        return Results.Forbid();
    }

    logger.LogInformation($"Intento de crear usuario. Creador desde BD: {user.Role}, Creador desde Token: {roleClaim}, Nuevo rol: {request.Role}");

    // Verificar que el rol del token coincide con el de la BD (validación de seguridad)
    if (!string.IsNullOrEmpty(roleClaim))
    {
        var roleFromToken = roleClaim;
        var roleFromDb = user.Role.ToString();
        
        if (roleFromToken != roleFromDb)
        {
            logger.LogWarning($"Inconsistencia de roles: Token={roleFromToken}, BD={roleFromDb}");
        }
    }

    // Verificar que el usuario tiene permiso para crear usuarios
    // SuperUsuario, Admin, Usuario Final y Empresa pueden crear usuarios (con restricciones)
    if (user.Role != Data.Models.UserRole.SuperUsuario && 
        user.Role != Data.Models.UserRole.Admin &&
        user.Role != Data.Models.UserRole.Final &&
        user.Role != Data.Models.UserRole.Empresa)
    {
        logger.LogWarning($"Usuario {user.Role} no tiene permiso para crear usuarios");
        return Results.Forbid();
    }

    // Validación adicional: Solo SuperUsuario y Admin pueden crear Administradores
    if (request.Role == Data.Models.UserRole.Admin)
    {
        if (user.Role != Data.Models.UserRole.SuperUsuario && user.Role != Data.Models.UserRole.Admin)
        {
            logger.LogWarning($"Usuario {user.Role} intentó crear un Administrador sin permiso");
            return Results.Json(new { message = "Solo SuperUsuario y Administradores pueden crear usuarios Administradores." }, statusCode: 403);
        }
    }

    var newUser = await authService.CreateUserAsync(request, userId);
    if (newUser == null)
    {
        logger.LogWarning($"Error al crear usuario. Email: {request.Email}, Rol solicitado: {request.Role}, Creador: {user.Role}");
        return Results.BadRequest(new { message = "Error al crear usuario. El email puede estar en uso o no tienes permisos para crear este tipo de usuario." });
    }

    logger.LogInformation($"Usuario creado exitosamente: {newUser.Email}, Rol: {newUser.Role}");
    return Results.Created($"/api/admin/users/{newUser.Id}", newUser);
})
.WithName("CreateUser")
.Produces<UserDto>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status403Forbidden);

// Actualizar estado de usuario (Solo SuperUsuario)
app.MapPut("/api/admin/users/{id}/status", [Authorize] async (int id, HttpContext context, IAuthService authService, bool isActive) =>
{
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var user = await authService.GetUserByIdAsync(userId);
    
    if (user == null || user.Role != Data.Models.UserRole.SuperUsuario)
    {
        return Results.Forbid();
    }

    var result = await authService.UpdateUserStatusAsync(id, isActive);
    if (!result)
    {
        return Results.NotFound();
    }

    return Results.Ok(new { message = "Estado actualizado exitosamente" });
})
.WithName("UpdateUserStatus")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status403Forbidden);

// Endpoint WebSocket para chat en tiempo real
app.MapGet("/chat/ws", async (HttpContext ctx, OrchestratorApp orch) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var buffer = new byte[64 * 1024];
    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
    var input = JsonSerializer.Deserialize<ChatMessage>(text)!;
    var flow = new[] { AgentRole.UR, AgentRole.PM, AgentRole.PO, AgentRole.UX, AgentRole.Dev };

    await foreach (var msg in orch.RunAsync(input.ConversationId, input, flow))
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(msg);
        await ws.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
})
.WithName("ChatWebSocket");

app.Run();

public record CreateProjectRequest(string Name, string Description);

public partial class Program { }
