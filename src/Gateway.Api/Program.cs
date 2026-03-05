using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Knowledge;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Configurar Entity Framework Core con PostgreSQL
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5433;Database=multiagent;Username=appuser;Password=appsecret";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(defaultConn);
}, optionsLifetime: ServiceLifetime.Singleton);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(defaultConn);
});

// ========== CONFIGURAR ASP.NET CORE IDENTITY ==========
// Configurado en paralelo con el sistema actual (BCrypt)
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Configuración de contraseña
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = false;
    options.Password.RequiredUniqueChars = 3;
    
    // Configuración de bloqueo de cuenta
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // Configuración de usuario
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    
    // Configuración de inicio de sesión
    options.SignIn.RequireConfirmedEmail = false; // Cambiar a true en producción
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configurar JWT Authentication
// Nota: Identity ya configuró su esquema de autenticación, pero seguimos usando JWT para API
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-secret-key-min-32-characters-long-for-security-purposes";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MultiAgentSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MultiAgentSystem";

// Configurar autenticación para usar JWT como esquema por defecto
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // Eliminar delay de 5 minutos predeterminado
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "SuperUsuario"));
    options.AddPolicy("AdminOrSuperUser", policy => policy.RequireRole("Admin", "SuperUsuario"));
    // Solo SuperUsuario puede modificar configuraciones internas (behaviors, configurations, admin)
    options.AddPolicy(AuthorizationRoles.SuperUserOnlyPolicy, policy => policy.RequireRole(AuthorizationRoles.SuperUsuario));
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

// Configurar Embeddings (RAG)
var embeddingsSection = builder.Configuration.GetSection("Embeddings");
var embeddingsOptions = new EmbeddingOptions
{
    BaseUrl = embeddingsSection.GetValue<string>("BaseUrl") ?? baseUrl,
    Model = embeddingsSection.GetValue<string>("Model") ?? "nomic-embed-text",
    TimeoutSeconds = embeddingsSection.GetValue<int?>("TimeoutSeconds") ?? timeoutSeconds,
    Key = embeddingsSection.GetValue<string>("Key") ?? builder.Configuration["OpenAI:Key"]
};

builder.Services.AddSingleton(embeddingsOptions);

builder.Services.AddHttpClient<OpenAiEmbeddingClient>(c =>
{
    c.BaseAddress = new Uri(embeddingsOptions.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(embeddingsOptions.TimeoutSeconds);
    if (!string.IsNullOrEmpty(embeddingsOptions.Key))
    {
        c.DefaultRequestHeaders.Add("Authorization", $"Bearer {embeddingsOptions.Key}");
    }
});

builder.Services.AddSingleton<IEmbeddingClient>(provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OpenAiEmbeddingClient));
    var opts = provider.GetRequiredService<EmbeddingOptions>();
    return new OpenAiEmbeddingClient(httpClient, opts.Model);
});

var ragSection = builder.Configuration.GetSection("Rag");
var ragOptions = new PgVectorOptions
{
    ConnectionString = ragSection.GetValue<string>("ConnectionString") ?? defaultConn,
    Schema = ragSection.GetValue<string>("Schema") ?? "public",
    TableName = ragSection.GetValue<string>("TableName") ?? "knowledge_chunks",
    EmbeddingDimensions = ragSection.GetValue<int?>("EmbeddingDimensions") ?? 768,
    MaxContextChars = ragSection.GetValue<int?>("MaxContextChars") ?? 4000,
    IvfLists = ragSection.GetValue<int?>("IvfLists") ?? 100
};

builder.Services.AddSingleton(ragOptions);
builder.Services.AddSingleton<IRetriever, PgVectorRetriever>();
builder.Services.AddSingleton<IKnowledgeStore, PgVectorKnowledgeStore>();

// Observabilidad / Telemetría (OpenTelemetry)
var otelSection = builder.Configuration.GetSection("OpenTelemetry");
var otelEnabled = otelSection.GetValue<bool>("Enabled", false);
if (otelEnabled)
{
    var serviceName = otelSection.GetValue<string>("ServiceName") ?? "Gateway.Api";
    var otlpEndpoint = otelSection.GetValue<string>("OtlpEndpoint");

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName))
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            tracing.AddEntityFrameworkCoreInstrumentation();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
            }
        })
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddHttpClientInstrumentation();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
            }
        });
}

// Registrar servicios
// Configurar AuthService con switch entre BCrypt (actual) e Identity (nuevo)
var useIdentityAuth = builder.Configuration.GetValue<bool>("UseIdentityAuth", false);

if (useIdentityAuth)
{
    builder.Services.AddScoped<IAuthService, AuthServiceIdentity>();
    Console.WriteLine("✅ Usando AuthServiceIdentity (ASP.NET Core Identity)");
}
else
{
builder.Services.AddScoped<IAuthService, AuthService>();
    Console.WriteLine("✅ Usando AuthService original (BCrypt)");
}

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAgentLoggingService, AgentLoggingService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IUserMigrationService, UserMigrationService>();
builder.Services.AddScoped<BehaviorService>();
builder.Services.AddScoped<IBehaviorService>(sp => sp.GetRequiredService<BehaviorService>());
builder.Services.AddScoped<IBehaviorProvider>(sp => sp.GetRequiredService<BehaviorService>());
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<IDevFlowService, DevFlowService>();
builder.Services.AddSingleton<IDevFlowPipeline, DevFlowPipeline>();
builder.Services.AddScoped<IDevFlowAgentDispatcher, DevFlowAgentDispatcher>();

// Registrar agentes
builder.Services.AddScoped<IAgent, UrAgent>();
builder.Services.AddScoped<IAgent, PmAgent>();
builder.Services.AddScoped<IAgent, PoAgent>();
builder.Services.AddScoped<IAgent, DevAgent>();
builder.Services.AddScoped<IAgent, UxAgent>();

// Registrar Orchestrator con logging (Scoped porque usa IAgentLoggingService que es Scoped)
builder.Services.AddScoped<OrchestratorApp>(provider =>
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

async Task<int> GetLegacyUserIdAsync(ClaimsPrincipal user, IServiceProvider services)
{
    if (!useIdentityAuth)
    {
        return int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    var legacyClaim = user.FindFirstValue("legacy_user_id");
    if (!string.IsNullOrEmpty(legacyClaim) && int.TryParse(legacyClaim, out var lid))
    {
        return lid;
    }

    var email = user.FindFirstValue(ClaimTypes.Email);
    if (string.IsNullOrWhiteSpace(email))
    {
        throw new InvalidOperationException("El token no contiene ClaimTypes.Email para resolver legacy_user_id.");
    }

    var emailNorm = email.Trim().ToLowerInvariant();
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var legacy = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm);
    if (legacy == null)
    {
        legacy = new User
        {
            Email = emailNorm,
            Name = user.FindFirstValue(ClaimTypes.Name) ?? "Usuario",
            Role = UserRole.Final,
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"))
        };
        db.Users.Add(legacy);
        await db.SaveChangesAsync();
    }
    return legacy.Id;
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Aplicar migraciones de EF Core (más rápido y consistente que EnsureCreated)
    db.Database.Migrate();
}

// NOTA: UseDefaultFiles() y UseStaticFiles() fueron eliminados porque Gateway.Api es solo una API REST.
// El frontend ahora está en Gateway.Blazor (proyecto separado).
// Si se necesita servir archivos estáticos (p.ej. para Swagger), se pueden agregar de forma condicional.

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Multi-Agent Gateway API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ========== ENDPOINT RAÍZ ==========
// Endpoint informativo para la raíz - indica que es una API REST
app.MapGet("/", () => Results.Json(new 
{ 
    message = "Multi-Agent Gateway API",
    version = "v1",
    documentation = "/swagger",
    endpoints = new
    {
        auth = "/api/auth/login",
        projects = "/api/projects",
        chat = "/api/chat/run",
        configurations = "/api/configurations"
    }
}))
.WithName("Root")
.Produces(StatusCodes.Status200OK);

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
        return Results.BadRequest(new { message = "El email ya existe en la base de datos. Por favor, usa otro email o inicia sesión." });
    }
    return Results.Ok(result);
})
.WithName("Register")
.Produces<UserDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// Olvidé mi contraseña
app.MapPost("/api/auth/forgot-password", async (IPasswordRecoveryService recoveryService, ForgotPasswordRequest request) =>
{
    await recoveryService.StartRecoveryAsync(request.Email);
    return Results.Ok(new { message = "Si el email existe, se enviará un código de recuperación" });
})
.WithName("ForgotPassword")
.Produces(StatusCodes.Status200OK);

// Verificar código
app.MapPost("/api/auth/verify-code", async (IPasswordRecoveryService recoveryService, VerifyCodeRequest request) =>
{
    var ok = await recoveryService.VerifyCodeAsync(request.Email, request.Code);
    return ok ? Results.Ok(new { valid = true }) : Results.BadRequest(new { message = "Código inválido o expirado" });
})
.WithName("VerifyCode")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// Reset password con código
app.MapPost("/api/auth/reset-password", async (IPasswordRecoveryService recoveryService, ResetPasswordWithCodeRequest request) =>
{
    var result = await recoveryService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword);
    if (!result)
    {
        return Results.BadRequest(new { message = "Código inválido o expirado" });
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
    var userId = await GetLegacyUserIdAsync(context.User, context.RequestServices);
    var projects = await projectService.GetUserProjectsAsync(userId);
    return Results.Ok(projects);
})
.WithName("GetUserProjects")
.Produces<List<ProjectDto>>(StatusCodes.Status200OK);

// Crear proyecto
app.MapPost("/api/projects", [Authorize] async (HttpContext context, IProjectService projectService, CreateProjectRequest request) =>
{
    var userId = await GetLegacyUserIdAsync(context.User, context.RequestServices);
    var project = await projectService.CreateProjectAsync(userId, request.Name, request.Description);
    return Results.Created($"/api/projects/{project.Id}", project);
})
.WithName("CreateProject")
.Produces<ProjectDto>(StatusCodes.Status201Created);

// Obtener proyecto por ID
app.MapGet("/api/projects/{id}", [Authorize] async (int id, HttpContext context, IProjectService projectService) =>
{
    var userId = await GetLegacyUserIdAsync(context.User, context.RequestServices);
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
    var userId = await GetLegacyUserIdAsync(context.User, context.RequestServices);
    
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
// Solo SuperUsuario puede acceder a configuraciones internas

// Obtener todas las configuraciones
app.MapGet("/api/configurations", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (IConfigurationService configService) =>
{
    var configs = await configService.GetAllConfigurationsAsync();
    return Results.Ok(configs);
})
.WithName("GetAllConfigurations")
.Produces<List<SystemConfigurationDto>>(StatusCodes.Status200OK);

// Obtener configuraciones por tipo
app.MapGet("/api/configurations/{type}", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (string type, IConfigurationService configService) =>
{
    var configs = await configService.GetConfigurationsByTypeAsync(type);
    return Results.Ok(configs);
})
.WithName("GetConfigurationsByType")
.Produces<List<SystemConfigurationDto>>(StatusCodes.Status200OK);

// Crear configuración
app.MapPost("/api/configurations", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (
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

// Actualizar configuración
app.MapPut("/api/configurations/{id}", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (
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

// Activar/Desactivar configuración
app.MapPatch("/api/configurations/{id}/toggle", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (
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

// Eliminar configuración
app.MapDelete("/api/configurations/{id}", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (
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

// ========== ENDPOINTS DE BEHAVIORS ==========
// Solo SuperUsuario puede acceder a behaviors (configuración interna)
app.MapGet("/api/behaviors", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (IBehaviorService behaviorService, CancellationToken ct) =>
{
    var list = await behaviorService.GetAllAsync(ct);
    return Results.Ok(list);
})
.WithName("GetAllBehaviors")
.Produces<List<BehaviorDto>>(StatusCodes.Status200OK);

app.MapGet("/api/behaviors/{role}", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (AgentRole role, IBehaviorService behaviorService, CancellationToken ct) =>
{
    var behavior = await behaviorService.GetByRoleAsync(role, ct);
    return Results.Ok(behavior);
})
.WithName("GetBehaviorByRole")
.Produces<BehaviorDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/behaviors", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (BehaviorUpsertRequest request, IBehaviorService behaviorService, CancellationToken ct) =>
{
    var behavior = await behaviorService.UpsertAsync(request, ct);
    return Results.Ok(behavior);
})
.WithName("UpsertBehavior")
.Produces<BehaviorDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// ========== ENDPOINTS DEVFLOW ==========
// Solo SuperUsuario puede crear y gestionar DevFlow runs

app.MapPost("/api/devflow/runs", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (
    HttpContext context,
    IDevFlowService devFlowService,
    CreateDevFlowRunRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { message = "El título es requerido" });
    }

    var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var createdByUserId))
    {
        return Results.Json(new { message = "Token inválido" }, statusCode: 403);
    }

    var run = await devFlowService.CreateRunAsync(request, createdByUserId);
    if (run == null)
    {
        return Results.BadRequest(new { message = "El ProjectId no existe" });
    }

    return Results.Created($"/api/devflow/runs/{run.Id}", run);
})
.WithName("CreateDevFlowRun")
.Produces<DevFlowRunResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden);

app.MapGet("/api/devflow/runs", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (
    IDevFlowService devFlowService,
    int? projectId,
    int? status,
    int? stage,
    int? createdByUserId,
    DateTime? fromDate,
    DateTime? toDate,
    int? page,
    int? pageSize) =>
{
    if (status.HasValue && !Enum.IsDefined(typeof(DevFlowRunStatus), status.Value))
        return Results.BadRequest(new { message = "status inválido" });
    if (stage.HasValue && !Enum.IsDefined(typeof(DevFlowStage), stage.Value))
        return Results.BadRequest(new { message = "stage inválido" });

    var query = new DevFlowRunsQueryParams
    {
        ProjectId = projectId,
        Status = status.HasValue ? (DevFlowRunStatus)status.Value : null,
        Stage = stage.HasValue ? (DevFlowStage)stage.Value : null,
        CreatedByUserId = createdByUserId,
        FromDate = fromDate,
        ToDate = toDate,
        Page = page ?? 1,
        PageSize = pageSize ?? 20
    };

    if (query.PageSize < 1 || query.PageSize > 100)
        return Results.BadRequest(new { message = "pageSize debe estar entre 1 y 100" });
    if (query.Page < 1)
        return Results.BadRequest(new { message = "page debe ser mayor a 0" });

    var result = await devFlowService.GetRunsAsync(query);
    return Results.Ok(result);
})
.WithName("ListDevFlowRuns")
.Produces<PagedResponse<DevFlowRunListItem>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden);

app.MapGet("/api/devflow/runs/{id}", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (int id, IDevFlowService devFlowService) =>
{
    var run = await devFlowService.GetRunByIdAsync(id);
    return run == null ? Results.NotFound() : Results.Ok(run);
})
.WithName("GetDevFlowRun")
.Produces<DevFlowRunDetailResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden);

app.MapPost("/api/devflow/runs/{id}/execute-stage", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (int id, IDevFlowService devFlowService, ExecuteStageRequest? request) =>
{
    var req = request ?? new ExecuteStageRequest();
    var result = await devFlowService.ExecuteStageAsync(id, req);

    if (result.IsSuccess)
        return Results.Ok(result.Response);

    return Results.Json(new { message = result.ErrorMessage }, statusCode: result.HttpStatusCode);
})
.WithName("ExecuteDevFlowStage")
.Produces<ExecuteStageResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status409Conflict);

app.MapPost("/api/devflow/runs/{id}/approve", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (int id, HttpContext context, IDevFlowService devFlowService, ApproveGateRequest request) =>
{
    var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var decidedByUserId))
    {
        return Results.Json(new { message = "Token inválido" }, statusCode: 403);
    }

    var result = await devFlowService.ApproveGateAsync(id, request, decidedByUserId);

    if (result.IsSuccess)
        return Results.Ok(result.Response);

    return Results.Json(new { message = result.ErrorMessage }, statusCode: result.HttpStatusCode);
})
.WithName("ApproveDevFlowGate")
.Produces<ApproveGateResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/api/devflow/runs/{id}/branch-plan", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (int id, IDevFlowService devFlowService, string? format) =>
{
    var plan = await devFlowService.GetBranchPlanExportAsync(id);

    if (plan == null)
        return Results.NotFound(new { message = "Run no existe o no tiene Branch Plan asociado." });

    var fmt = (format ?? "json").Trim().ToLowerInvariant();
    if (fmt == "md" || fmt == "markdown")
    {
        var markdown = BranchPlanMarkdownFormatter.ToMarkdown(plan, id);
        return Results.Text(markdown, "text/markdown");
    }

    return Results.Json(plan);
})
.WithName("GetBranchPlanExport")
.Produces<BranchPlanExportDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status404NotFound);

// ========== ENDPOINTS ADMIN INTERNO ==========
// Solo SuperUsuario puede acceder a administración interna

// Listar todos los proyectos
app.MapGet("/api/admin/projects", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (IProjectService projectService) =>
{
    var projects = await projectService.GetAllProjectsAsync();
    return Results.Ok(projects);
})
.WithName("GetAllProjects")
.Produces<List<ProjectDto>>(StatusCodes.Status200OK);

// Obtener logs de una conversación
app.MapGet("/api/admin/logs/{conversationId}", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (string conversationId, IAgentLoggingService loggingService) =>
{
    var logs = await loggingService.GetConversationLogsAsync(conversationId);
    return Results.Ok(logs);
})
.WithName("GetConversationLogs")
.Produces<List<AgentLogEntry>>(StatusCodes.Status200OK);

// Obtener todos los logs
app.MapGet("/api/admin/logs", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (IAgentLoggingService loggingService, int? limit) =>
{
    var logs = await loggingService.GetAllLogsAsync(limit);
    return Results.Ok(logs);
})
.WithName("GetAllLogs")
.Produces<List<AgentLogEntry>>(StatusCodes.Status200OK);

// Migrar usuarios a Identity - Endpoint temporal para migración
app.MapPost("/api/admin/migrate-users", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (IUserMigrationService migrationService) =>
{
    var result = await migrationService.MigrateUsersToIdentityAsync();
    return Results.Ok(result);
})
.WithName("MigrateUsersToIdentity")
.Produces<MigrationResult>(StatusCodes.Status200OK);

// ========== ENDPOINTS USUARIOS ADMIN ==========

// Listar todos los usuarios
app.MapGet("/api/admin/users", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (IAuthService authService) =>
{
    var users = await authService.GetAllUsersAsync();
    return Results.Ok(users);
})
.WithName("GetAllUsers")
.Produces<List<UserDto>>(StatusCodes.Status200OK);

// Crear usuario (solo SuperUsuario)
app.MapPost("/api/admin/users", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (HttpContext context, IAuthService authService, CreateUserRequest request, ILogger<Program> logger) =>
{
    var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userIdClaim))
    {
        return Results.Json(new { message = "Token inválido" }, statusCode: 403);
    }
    var userId = int.Parse(userIdClaim);

    var newUser = await authService.CreateUserAsync(request, userId);
    if (newUser == null)
    {
        logger.LogWarning($"Error al crear usuario. Email: {request.Email}, Rol solicitado: {request.Role}");
        return Results.BadRequest(new
        {
            message = "Error al crear usuario. Verifica que el email no exista, el rol sea permitido y la contraseña cumpla: 8+ caracteres, 1 mayúscula, 1 número, 3 caracteres únicos."
        });
    }

    logger.LogInformation($"Usuario creado exitosamente: {newUser.Email}, Rol: {newUser.Role}");
    return Results.Created($"/api/admin/users/{newUser.Id}", newUser);
})
.WithName("CreateUser")
.Produces<UserDto>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status403Forbidden);

// Actualizar estado de usuario
app.MapPut("/api/admin/users/{id}/status", [Authorize(Policy = AuthorizationRoles.SuperUserOnlyPolicy)] async (int id, IAuthService authService, bool isActive) =>
{
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
