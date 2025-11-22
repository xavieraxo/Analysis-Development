using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using Gateway.Blazor.Services;
using Gateway.Blazor.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true; // Para debugging
    options.DisconnectedCircuitMaxRetained = 100;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
});

// MudBlazor
builder.Services.AddMudServices();

// HttpClient para API
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8094") 
});

// Servicios personalizados
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApiService, ApiService>();

// Autenticación y autorización para Blazor
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// WeatherForecast (puede eliminarse después)
builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // No usar HSTS en Docker sin HTTPS
    // app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// No usar redirección HTTPS en Docker sin certificados
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

// Nota: UseAuthentication/UseAuthorization no son necesarios aquí para Blazor Server
// ya que la autenticación se maneja a través de AuthenticationStateProvider

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
