using System.Text;
using System.Text.Json;
using Npgsql;
using Orchestrator.Domain;
using Orchestrator.Services;

var builder = WebApplication.CreateBuilder(args);

var webhookSecret = builder.Configuration["Orchestrator:WebhookSecret"] ?? "";

// Persistencia: PostgreSQL si hay connection string; si no (tests), los
// registros se reemplazan desde fuera.
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("OrchestratorDb")))
{
    builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
        NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("OrchestratorDb")!));
    builder.Services.AddSingleton<IAgentJobRepository, PostgresAgentJobRepository>();
}

builder.Services.AddSingleton<IDeliveryTracker, InMemoryDeliveryTracker>();
builder.Services.AddSingleton<IGitHubWorkflowClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var token = configuration["Orchestrator:GitHub:Token"]
        ?? throw new InvalidOperationException("Falta Orchestrator:GitHub:Token (user-secrets en dev, Key Vault en prod — Article III).");
    var client = new HttpClient
    {
        BaseAddress = new Uri("https://api.github.com/"),
        Timeout = TimeSpan.FromSeconds(30),
    };
    return new GitHubWorkflowClient(
        client,
        configuration["Orchestrator:GitHub:Owner"] ?? throw new InvalidOperationException("Falta Orchestrator:GitHub:Owner"),
        configuration["Orchestrator:GitHub:Repository"] ?? throw new InvalidOperationException("Falta Orchestrator:GitHub:Repository"),
        token);
});
builder.Services.AddSingleton<JobDispatcher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<JobDispatcher>());
builder.Services.AddHostedService<RunReconciler>();

var app = builder.Build();

// GET /health — liveness, mismo patrón que Api (specs/health-check/).
app.MapGet("/health", () => new HealthResponse("healthy", DateTime.UtcNow))
    .WithName("GetHealth");

// POST /webhooks/github — ingest de webhooks (HMAC obligatorio).
app.MapPost("/webhooks/github", async (HttpContext context, IAgentJobRepository jobs, IDeliveryTracker deliveries, ILogger<Program> logger) =>
{
    var deliveryId = context.Request.Headers["X-GitHub-Delivery"].FirstOrDefault() ?? "";
    var eventName = context.Request.Headers["X-GitHub-Event"].FirstOrDefault() ?? "";
    var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();

    context.Request.EnableBuffering();
    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
    var body = await reader.ReadToEndAsync();
    context.Request.Body.Position = 0;

    if (!WebhookVerifier.IsValidSignature(body, signature, webhookSecret))
    {
        return Results.Unauthorized();
    }

    // Idempotencia por delivery_id (specs/orchestrator/architecture.md, decisión 3).
    if (string.IsNullOrEmpty(deliveryId) || !await deliveries.TryMarkProcessedAsync(deliveryId, context.RequestAborted))
    {
        return Results.Ok(new { ignored = "duplicate" });
    }

    using var payload = JsonDocument.Parse(body);

    if (eventName == "issues"
        && payload.RootElement.TryGetProperty("action", out var action)
        && action.GetString() == "labeled"
        && payload.RootElement.TryGetProperty("label", out var label)
        && label.GetProperty("name").GetString() == "ai-ready")
    {
        var issue = payload.RootElement.GetProperty("issue");
        var issueNumber = issue.GetProperty("number").GetInt32();
        var issueTitle = issue.GetProperty("title").GetString() ?? "";
        var issueBody = issue.GetProperty("body").GetString() ?? "";
        var specPath = ExtractSpecPath(issueTitle) ?? ExtractSpecPath(issueBody);

        if (specPath is null)
        {
            return Results.Ok(new { ignored = "no-spec-path" });
        }

        var job = AgentJob.FromIssueLabeled(deliveryId, issueNumber, issueTitle, specPath, DateTimeOffset.UtcNow);
        var created = await jobs.TryAddAsync(job, context.RequestAborted);
        return Results.Ok(new { created, jobId = job.Id });
    }

    return Results.Ok(new { ignored = "unhandled-event" });
});

// GET /jobs?state=Pending — observabilidad (specs/orchestrator/acceptance.md).
app.MapGet("/jobs", async (string state, IAgentJobRepository repository, CancellationToken cancellationToken) =>
{
    if (!Enum.TryParse<JobState>(state, ignoreCase: true, out var jobState))
    {
        return Results.BadRequest(new { error = "state inválido" });
    }

    var list = await repository.ListByStateAsync(jobState, cancellationToken);
    return Results.Ok(list.Select(ToDto));
});

app.MapGet("/jobs/{id:guid}", async (Guid id, IAgentJobRepository repository, CancellationToken cancellationToken) =>
{
    var job = await repository.GetAsync(id, cancellationToken);
    return job is null ? Results.NotFound() : Results.Ok(ToDto(job));
});

app.Run();

static string? ExtractSpecPath(string text)
{
    var match = System.Text.RegularExpressions.Regex.Match(
        text, @"specs/[\w\-]+/?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    return match.Success ? match.Value.TrimEnd('/') : null;
}

static object ToDto(AgentJob job) => new
{
    job.Id,
    Type = job.Type.ToString(),
    State = job.State.ToString(),
    Trigger = job.Trigger.ToString(),
    job.Payload,
    job.AttemptCount,
    job.GithubRunId,
    job.CreatedAt,
    job.DispatchedAt,
    job.CompletedAt,
};

/// <summary>Tracker in-memory de deliveries. Suficiente para iteración 1
/// (un solo host); con deploy multi-instancia pasa a la tabla
/// processed_deliveries.</summary>
public class InMemoryDeliveryTracker : IDeliveryTracker
{
    private readonly HashSet<string> processed = new();

    public Task<bool> TryMarkProcessedAsync(string deliveryId, CancellationToken cancellationToken)
    {
        lock (processed)
        {
            return Task.FromResult(processed.Add(deliveryId));
        }
    }
}

record HealthResponse(string Status, DateTime TimestampUtc);

// Necesario para WebApplicationFactory en tests.
public partial class Program { }
