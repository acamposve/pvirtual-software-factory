using Orchestrator.Domain;
using Xunit;

namespace Orchestrator.Tests;

/// <summary>
/// Tests de la máquina de estados (specs/orchestrator/acceptance.md:
/// retry hasta 3 y escalada; invariantes de domain.md).
/// </summary>
public class AgentJobTests
{
    private static AgentJob NewJob() =>
        AgentJob.FromIssueLabeled("d-1", 42, "test", "specs/demo/", DateTimeOffset.UtcNow);

    [Fact]
    public void Nuevo_job_arranca_Pending()
    {
        var job = NewJob();
        Assert.Equal(JobState.Pending, job.State);
        Assert.Equal(0, job.AttemptCount);
    }

    [Fact]
    public void Issue_sin_spec_path_es_invalido()
    {
        Assert.Throws<ArgumentException>(
            () => AgentJob.FromIssueLabeled("d-1", 1, "t", "", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Dispatch_valido_desde_Pending()
    {
        var job = NewJob();
        job.MarkDispatched(123, DateTimeOffset.UtcNow);
        Assert.Equal(JobState.Dispatched, job.State);
        Assert.Equal(123, job.GithubRunId);
    }

    [Fact]
    public void Transicion_invalida_lanza()
    {
        var job = NewJob();
        // Succeeded directo desde Pending no es válido.
        Assert.Throws<InvalidOperationException>(() => job.MarkSucceeded(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Fallo_con_presupuesto_pasa_a_Retrying()
    {
        var job = NewJob();
        job.MarkDispatched(1, DateTimeOffset.UtcNow);
        job.MarkRunning(DateTimeOffset.UtcNow);
        job.MarkFailed(DateTimeOffset.UtcNow);

        Assert.Equal(JobState.Retrying, job.State);
        Assert.Equal(1, job.AttemptCount);
    }

    [Fact]
    public void Tres_fallos_agotan_presupuesto_y_escalate()
    {
        var job = NewJob();
        job.MarkDispatched(1, DateTimeOffset.UtcNow);
        job.MarkRunning(DateTimeOffset.UtcNow);

        // 3 ciclos de fallo: Failed→Retrying→re-dispatch→Failed→...→Escalated.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            job.MarkFailed(DateTimeOffset.UtcNow);
            if (job.State == JobState.Retrying)
            {
                job.MarkRetrying(2, DateTimeOffset.UtcNow);
                job.MarkRunning(DateTimeOffset.UtcNow);
            }
        }

        Assert.Equal(JobState.Escalated, job.State);
        Assert.Equal(3, job.AttemptCount);
        Assert.NotNull(job.EscalationComment);
    }

    [Fact]
    public void Nunca_hay_cuarto_intento()
    {
        var job = NewJob();
        job.MarkDispatched(1, DateTimeOffset.UtcNow);
        job.MarkRunning(DateTimeOffset.UtcNow);

        // 3 fallos seguidos (sin re-dispatch intermedio): el 3ro escala.
        job.MarkFailed(DateTimeOffset.UtcNow);
        job.MarkFailed(DateTimeOffset.UtcNow);
        job.MarkFailed(DateTimeOffset.UtcNow);

        Assert.Equal(JobState.Escalated, job.State);

        // Cuarto fallo: transición inválida desde Escalated.
        Assert.Throws<InvalidOperationException>(() => job.MarkFailed(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Reconciliacion_Running_a_Succeeded()
    {
        var job = NewJob();
        job.MarkDispatched(7, DateTimeOffset.UtcNow);
        job.MarkRunning(DateTimeOffset.UtcNow);
        job.MarkSucceeded(DateTimeOffset.UtcNow);

        Assert.Equal(JobState.Succeeded, job.State);
        Assert.NotNull(job.CompletedAt);
    }
}
