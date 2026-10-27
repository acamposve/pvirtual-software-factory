namespace Orchestrator.Domain;

/// <summary>
/// Aggregate root: un trabajo de agente con su máquina de estados.
/// Las transiciones están encapsuladas acá — no hay setters libres
/// (specs/orchestrator/domain.md, invariantes).
/// </summary>
public class AgentJob
{
    public Guid Id { get; }

    public JobType Type { get; }

    public JobState State { get; private set; }

    public TriggerType Trigger { get; }

    /// <summary>Datos del trabajo: issue number, spec path, etc.</summary>
    public Dictionary<string, string> Payload { get; }

    public int AttemptCount { get; private set; }

    /// <summary>Id de delivery de GitHub — garantiza idempotencia (único).</summary>
    public string DeliveryId { get; }

    /// <summary>Run de Actions asociado, una vez despachado.</summary>
    public long? GithubRunId { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Comentario de escalada a publicar en el issue de origen.</summary>
    public string? EscalationComment { get; private set; }

    /// <summary>Factory: issue etiquetado ai-ready.</summary>
    public static AgentJob FromIssueLabeled(
        string deliveryId,
        int issueNumber,
        string issueTitle,
        string specPath,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(specPath))
        {
            throw new ArgumentException("El issue ai-ready debe indicar la carpeta de spec.", nameof(specPath));
        }

        return new AgentJob(
            id: Guid.NewGuid(),
            type: JobType.ImplementSpec,
            state: JobState.Pending,
            trigger: TriggerType.IssueLabeled,
            payload: new Dictionary<string, string>
            {
                ["issueNumber"] = issueNumber.ToString(),
                ["issueTitle"] = issueTitle,
                ["specPath"] = specPath,
            },
            attemptCount: 0,
            deliveryId: $"issue-labeled:{issueNumber}",
            githubRunId: null,
            createdAt: createdAt,
            dispatchedAt: null,
            completedAt: null)
        {
            // DeliveryId único por issue: un re-label del mismo issue no crea
            // un job nuevo (idempotencia de negocio, además de la de delivery).
        };
    }

    /// <summary>Rehidratación desde persistencia (único constructor público de instancia).</summary>
    public AgentJob(
        Guid id,
        JobType type,
        JobState state,
        TriggerType trigger,
        Dictionary<string, string> payload,
        int attemptCount,
        string deliveryId,
        long? githubRunId,
        DateTimeOffset createdAt,
        DateTimeOffset? dispatchedAt,
        DateTimeOffset? completedAt)
    {
        Id = id;
        Type = type;
        State = state;
        Trigger = trigger;
        Payload = payload;
        AttemptCount = attemptCount;
        DeliveryId = deliveryId;
        GithubRunId = githubRunId;
        CreatedAt = createdAt;
        DispatchedAt = dispatchedAt;
        CompletedAt = completedAt;
    }

    /// <summary>Dispatch: el orquestador invocó el workflow en GitHub.</summary>
    public void MarkDispatched(long githubRunId, DateTimeOffset at)
    {
        EnsureState(JobState.Pending, JobState.Retrying);
        GithubRunId = githubRunId;
        State = JobState.Dispatched;
        DispatchedAt = at;
    }

    /// <summary>El run de Actions arrancó efectivamente.</summary>
    public void MarkRunning(DateTimeOffset at)
    {
        EnsureState(JobState.Dispatched);
        State = JobState.Running;
    }

    public void MarkSucceeded(DateTimeOffset at)
    {
        EnsureState(JobState.Running, JobState.Dispatched);
        State = JobState.Succeeded;
        CompletedAt = at;
    }

    /// <summary>
    /// Run fallido: incrementa el intento. Si hay presupuesto (max 3) pasa a
    /// Retrying; si no, a Escalated con el comentario para el issue.
    /// </summary>
    public void MarkFailed(DateTimeOffset at, int maxAttempts = 3)
    {
        EnsureState(JobState.Running, JobState.Dispatched, JobState.Retrying);
        AttemptCount++;

        if (AttemptCount >= maxAttempts)
        {
            State = JobState.Escalated;
            EscalationComment =
                $"🚨 **Escalada humana** — el orquestador agotó los {maxAttempts} intentos " +
                $"del job `{Id}` y el run de GitHub Actions sigue fallando. " +
                "No se harán más reintentos automáticos (Constitution: máx. 3 reintentos autónomos).";
            CompletedAt = at;
        }
        else
        {
            State = JobState.Retrying;
        }
    }

    /// <summary>Re-dispatch tras un retry.</summary>
    public void MarkRetrying(long githubRunId, DateTimeOffset at)
    {
        EnsureState(JobState.Retrying);
        GithubRunId = githubRunId;
        State = JobState.Dispatched;
        DispatchedAt = at;
    }

    private void EnsureState(params JobState[] allowed)
    {
        if (!allowed.Contains(State))
        {
            throw new InvalidOperationException(
                $"Transición inválida: {State} no permite esta operación (permitidos: {string.Join(", ", allowed)}).");
        }
    }
}
