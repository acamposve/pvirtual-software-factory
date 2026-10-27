using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestrator.Domain;
using Orchestrator.Services;
using Xunit;

namespace Orchestrator.Tests;

/// <summary>
/// Tests del dispatcher con fakes: control de concurrencia y retry
/// (specs/orchestrator/acceptance.md).
/// </summary>
public class JobDispatcherTests
{
    private class FakeRepository : IAgentJobRepository
    {
        public List<AgentJob> Jobs { get; } = new();

        public Task<bool> TryAddAsync(AgentJob job, CancellationToken ct)
        {
            Jobs.Add(job);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<AgentJob>> TakeDispatchableAsync(int max, CancellationToken ct)
        {
            IReadOnlyList<AgentJob> ready = Jobs
                .Where(j => j.State is JobState.Pending or JobState.Retrying)
                .OrderBy(j => j.CreatedAt)
                .Take(max)
                .ToList();
            return Task.FromResult(ready);
        }

        public Task<IReadOnlyList<AgentJob>> ListByStateAsync(JobState state, CancellationToken ct)
        {
            IReadOnlyList<AgentJob> result = Jobs.Where(j => j.State == state).ToList();
            return Task.FromResult(result);
        }

        public Task<AgentJob?> GetAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Jobs.FirstOrDefault(j => j.Id == id));

        public Task UpdateAsync(AgentJob job, CancellationToken ct) => Task.CompletedTask;
    }

    private class FakeGitHub : IGitHubWorkflowClient
    {
        public int DispatchCalls { get; private set; }
        public List<int> EscalationComments { get; } = new();

        public Task<long> DispatchAsync(string workflowFileName, string refBranch, Dictionary<string, string> inputs, CancellationToken ct)
        {
            DispatchCalls++;
            return Task.FromResult(0L);
        }

        public Task<(string Status, string Conclusion)> GetRunStatusAsync(long runId, CancellationToken ct) =>
            Task.FromResult(("completed", "failure"));

        public Task CommentOnIssueAsync(int issueNumber, string body, CancellationToken ct)
        {
            EscalationComments.Add(issueNumber);
            return Task.CompletedTask;
        }
    }

    private static JobDispatcher NewDispatcher(IAgentJobRepository repo, IGitHubWorkflowClient gitHub, int maxConcurrent = 1)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Orchestrator:MaxConcurrentJobs"] = maxConcurrent.ToString(),
            })
            .Build();
        return new JobDispatcher(repo, gitHub, configuration, NullLogger<JobDispatcher>.Instance);
    }

    [Fact]
    public async Task Despacha_job_Pending()
    {
        var repository = new FakeRepository();
        var gitHub = new FakeGitHub();
        await repository.TryAddAsync(AgentJob.FromIssueLabeled("d1", 1, "t", "specs/demo/", DateTimeOffset.UtcNow), CancellationToken.None);

        var dispatched = await NewDispatcher(repository, gitHub).DispatchReadyJobsAsync(CancellationToken.None);

        Assert.Equal(1, dispatched);
        Assert.Equal(1, gitHub.DispatchCalls);
        Assert.Equal(JobState.Dispatched, repository.Jobs.Single().State);
    }

    [Fact]
    public async Task Concurrencia_1_deja_el_segundo_en_Pending()
    {
        var repository = new FakeRepository();
        var gitHub = new FakeGitHub();
        await repository.TryAddAsync(AgentJob.FromIssueLabeled("d1", 1, "a", "specs/a/", DateTimeOffset.UtcNow), CancellationToken.None);
        await repository.TryAddAsync(AgentJob.FromIssueLabeled("d2", 2, "b", "specs/b/", DateTimeOffset.UtcNow), CancellationToken.None);

        var dispatcher = NewDispatcher(repository, gitHub, maxConcurrent: 1);
        await dispatcher.DispatchReadyJobsAsync(CancellationToken.None);

        Assert.Equal(1, gitHub.DispatchCalls);
        Assert.Equal(1, repository.Jobs.Count(j => j.State == JobState.Dispatched));
        Assert.Equal(1, repository.Jobs.Count(j => j.State == JobState.Pending));
    }

    [Fact]
    public async Task Run_fallido_3_veces_escalata_y_comenta()
    {
        var repository = new FakeRepository();
        var gitHub = new FakeGitHub();
        var job = AgentJob.FromIssueLabeled("d1", 99, "t", "specs/demo/", DateTimeOffset.UtcNow);
        await repository.TryAddAsync(job, CancellationToken.None);

        var dispatcher = NewDispatcher(repository, gitHub);

        // 3 ciclos completos de fallo.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await dispatcher.DispatchReadyJobsAsync(CancellationToken.None);
            await dispatcher.ProcessRunResultAsync(job, "completed", "failure", CancellationToken.None);
            if (job.State == JobState.Retrying)
            {
                await dispatcher.DispatchReadyJobsAsync(CancellationToken.None);
            }
        }

        Assert.Equal(JobState.Escalated, job.State);
        Assert.Equal(3, job.AttemptCount);
        Assert.Single(gitHub.EscalationComments);
    }

    [Fact]
    public async Task Run_exitoso_termina_el_job()
    {
        var repository = new FakeRepository();
        var job = AgentJob.FromIssueLabeled("d1", 1, "t", "specs/demo/", DateTimeOffset.UtcNow);
        await repository.TryAddAsync(job, CancellationToken.None);

        var dispatcher = NewDispatcher(repository, new FakeGitHub());
        await dispatcher.DispatchReadyJobsAsync(CancellationToken.None);
        await dispatcher.ProcessRunResultAsync(job, "completed", "success", CancellationToken.None);

        Assert.Equal(JobState.Succeeded, job.State);
    }
}
