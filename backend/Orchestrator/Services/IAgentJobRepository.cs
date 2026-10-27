using Orchestrator.Domain;

namespace Orchestrator.Services;

/// <summary>Persistencia de jobs (PostgreSQL en producción, fake en tests).</summary>
public interface IAgentJobRepository
{
    /// <summary>Inserta un job. Devuelve false si el DeliveryId ya existe (idempotencia).</summary>
    Task<bool> TryAddAsync(AgentJob job, CancellationToken cancellationToken);

    /// <summary>Jobs listos para despachar (Pending o Retrying), con lock de cola.</summary>
    Task<IReadOnlyList<AgentJob>> TakeDispatchableAsync(int max, CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentJob>> ListByStateAsync(JobState state, CancellationToken cancellationToken);

    Task<AgentJob?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(AgentJob job, CancellationToken cancellationToken);
}

/// <summary>Registro de deliveries procesados (idempotencia de webhooks).</summary>
public interface IDeliveryTracker
{
    /// <summary>Devuelve true si el delivery es nuevo (y lo marca como procesado).</summary>
    Task<bool> TryMarkProcessedAsync(string deliveryId, CancellationToken cancellationToken);
}
