namespace Orchestrator.Domain;

/// <summary>Estados del ciclo de vida de un job de agente. Ver specs/orchestrator/domain.md.</summary>
public enum JobState
{
    Pending,
    Dispatched,
    Running,
    Succeeded,
    Failed,
    Retrying,
    Escalated,
}

/// <summary>Tipo de trabajo que el orquestador debe coordinar.</summary>
public enum JobType
{
    /// <summary>Implementar una spec a partir de un issue etiquetado ai-ready.</summary>
    ImplementSpec,
}

/// <summary>Origen del job (auditoría).</summary>
public enum TriggerType
{
    IssueLabeled,
}
