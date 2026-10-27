using Orchestrator.Domain;

namespace Orchestrator.Services;

/// <summary>
/// BackgroundService: al arrancar (y periódicamente) sincroniza jobs
/// Dispatched/Running con el estado real del run en GitHub API
/// (specs/orchestrator/architecture.md, decisión 5).
/// </summary>
public class RunReconciler : BackgroundService
{
    private readonly IAgentJobRepository repository;
    private readonly IGitHubWorkflowClient gitHub;
    private readonly JobDispatcher dispatcher;
    private readonly ILogger<RunReconciler> logger;

    public RunReconciler(
        IAgentJobRepository repository,
        IGitHubWorkflowClient gitHub,
        JobDispatcher dispatcher,
        ILogger<RunReconciler> logger)
    {
        this.repository = repository;
        this.gitHub = gitHub;
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Reconciliación al arranque (specs/orchestrator/spec.md): el servicio
        // puede reiniciarse sin perder trabajo.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error en el ciclo del reconciler");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        var dispatched = await repository.ListByStateAsync(JobState.Dispatched, cancellationToken);
        var running = await repository.ListByStateAsync(JobState.Running, cancellationToken);

        foreach (var job in dispatched.Concat(running))
        {
            if (job.GithubRunId is null or 0)
            {
                // El dispatch aún no resolvió run id (workflow_dispatch devuelve 204).
                // Se resuelve buscando el run más reciente del issue; si sigue sin
                // resolverse, se deja para el próximo ciclo.
                continue;
            }

            try
            {
                var (status, conclusion) = await gitHub.GetRunStatusAsync(job.GithubRunId.Value, cancellationToken);
                await dispatcher.ProcessRunResultAsync(job, status, conclusion, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo reconciliar el job {JobId}", job.Id);
            }
        }
    }
}
