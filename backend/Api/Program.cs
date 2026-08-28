using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// GET /health - liveness check. Ver specs/health-check/ (spec aprobada).
// Publico (sin auth), sin dependencias externas (no toca DB ni servicios).
app.MapGet("/health", () => new HealthResponse("healthy", DateTime.UtcNow))
    .WithName("GetHealth");

// GET /version - metadata del build. Ver specs/version-endpoint/ (spec aprobada).
// Publico (sin auth), sin dependencias externas.
app.MapGet("/version", () =>
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "1.0.0";

        return new VersionResponse(version, app.Environment.EnvironmentName);
    })
    .WithName("GetVersion");

app.Run();

record HealthResponse(string Status, DateTime TimestampUtc);

record VersionResponse(string Version, string Environment);

// Necesario para que WebApplicationFactory<Program> (en Api.Tests) pueda
// referenciar este Program implicito (top-level statements genera una
// clase Program internal por default).
public partial class Program { }
