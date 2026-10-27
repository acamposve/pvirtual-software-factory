namespace Orchestrator.Domain;

/// <summary>
/// Política de reintento: max 3 intentos, backoff exponencial
/// (specs/orchestrator/architecture.md, decisión 4 / Constitution).
/// </summary>
public class RetryPolicy
{
    public int MaxAttempts { get; }

    public RetryPolicy(int maxAttempts = 3)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Debe permitir al menos 1 intento.");
        }

        MaxAttempts = maxAttempts;
    }

    /// <summary>Delay antes del reintento N (1-based): 1min, 2min, 4min...</summary>
    public TimeSpan BackoffFor(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt) * 15));

    public bool HasBudget(int attemptCount) => attemptCount < MaxAttempts;
}
