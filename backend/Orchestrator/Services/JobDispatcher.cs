using Orchestrator.Domain;

namespace Orchestrator.Services;

/// <summary>
/// BackgroundService: toma jobs Pending/Retrying respetando MaxConcurrentJobs
/// y los despacha vía GitHub API (specs/orchestrator/architecture.md).
/// </summary>
public class JobDispatcher : BackgroundService
{
    private readonly IAgentJobRepository repository;
    private readonly IGitHubWorkflowClient gitHub;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<JobDispatcher> logger;
    private readonly int maxConcurrentJobs;
    private readonly RetryPolicy retryPolicy = new();

    public JobDispatcher(
        IAgentJobRepository repository,
        IGitHubWorkflowClient gitHub,
        IConfiguration configuration,
        ILogger<JobDispatcher> logger)
    {
        this.repository = repository;
        this.gitHub = gitHub;
        this.logger = logger;
        maxConcurrentJobs = configuration.GetValue<int?>("Orchestrator:MaxConcurrentJobs") ?? 1;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchReadyJobsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error en el ciclo del dispatcher");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);        }
    }

    /// <summary>Un ciclo de dispatch. Visible para tests.</summary>
    public async Task<int> DispatchReadyJobsAsync(CancellationToken cancellationToken)
    {
        var running = await repository.ListByStateAsync(JobState.Running, cancellationToken);
        var dispatched = await repository.ListByStateAsync(JobState.Dispatched, cancellationToken);
        var inFlight = running.Count + dispatched.Count;

        var slots = Math.Max(0, maxConcurrentJobs - inFlight);
        if (slots == 0)
        {
            return 0;
        }

        var ready = await repository.TakeDispatchableAsync(slots, cancellationToken);
        var dispatchedCount = 0;

        foreach (var job in ready)
        {
            try
            {
                var runId = await gitHub.DispatchAsync(
                    "developer-agent.yml",
                    "main",
                    new Dictionary<string, string>
                    {
                        ["issue_number"] = job.Payload["issueNumber"],
                        ["spec_path"] = job.Payload["specPath"],
                    },
                    cancellationToken);

                job.MarkDispatched(runId, DateTimeOffset.UtcNow);
                await repository.UpdateAsync(job, cancellationToken);
                dispatchedCount++;
                logger.LogInformation("Job {JobId} despachado (run {RunId})", job.Id, runId);
            }
            catch (Exception ex)
            {
                // GitHub API caída: el job queda Pending, reintento con backoff
                // (specs/orchestrator/spec.md, casos de error).
                logger.LogWarning(ex, "No se pudo despachar el job {JobId}; queda en cola", job.Id);
            }
        }

        return dispatchedCount;
    }

    /// <summary>Procesa el resultado de un run (llamado por el reconciler).</summary>
    public async Task ProcessRunResultAsync(AgentJob job, string status, string conclusion, CancellationToken cancellationToken)
    {
        switch (status)
        {
            case "completed" when conclusion == "success":
                job.MarkSucceeded(DateTimeOffset.UtcNow);
                logger.LogInformation("Job {JobId} Succeeded", job.Id);
                break;

            case "completed":
                job.MarkFailed(DateTimeOffset.UtcNow, retryPolicy.MaxAttempts);
                if (job.State == JobState.Escalated && job.EscalationComment is not null
                    && int.TryParse(job.Payload["issueNumber"], out var issue))
                {
                    await gitHub.CommentOnIssueAsync(issue, job.EscalationComment, cancellationToken);
                }
                logger.LogWarning("Job {JobId} falló (intento {Attempt}) → {State}", job.Id, job.AttemptCount, job.State);
                break;

            case "in_progress" or "queued" when job.State == JobState.Dispatched:
                job.MarkRunning(DateTimeOffset.UtcNow);
                break;

            default:
                return; // sin cambio de estado
        }

        await repository.UpdateAsync(job, cancellationToken);
    }
}
