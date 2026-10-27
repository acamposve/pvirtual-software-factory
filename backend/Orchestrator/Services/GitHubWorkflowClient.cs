using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Orchestrator.Services;

/// <summary>Abstracción para tests (specs/orchestrator/architecture.md, decisión 1).</summary>
public interface IGitHubWorkflowClient
{
    /// <summary>Dispara un workflow y devuelve el id del run creado (0 si no se pudo determinar).</summary>
    Task<long> DispatchAsync(string workflowFileName, string refBranch, Dictionary<string, string> inputs, CancellationToken cancellationToken);

    /// <summary>Estado actual de un run: "queued", "in_progress" o "completed".</summary>
    Task<(string Status, string Conclusion)> GetRunStatusAsync(long runId, CancellationToken cancellationToken);

    /// <summary>Comenta en un issue (escaladas).</summary>
    Task CommentOnIssueAsync(int issueNumber, string body, CancellationToken cancellationToken);
}

/// <summary>Cliente GitHub API mínimo (solo lo que el orquestador necesita).</summary>
public class GitHubWorkflowClient : IGitHubWorkflowClient
{
    private readonly HttpClient httpClient;
    private readonly string owner;
    private readonly string repository;

    public GitHubWorkflowClient(HttpClient httpClient, string owner, string repository, string token)
    {
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("pvirtual-orchestrator");
        httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        this.httpClient = httpClient;
        this.owner = owner;
        this.repository = repository;
    }

    public async Task<long> DispatchAsync(
        string workflowFileName, string refBranch, Dictionary<string, string> inputs, CancellationToken cancellationToken)
    {
        var url = $"repos/{owner}/{repository}/actions/workflows/{workflowFileName}/dispatches";
        var body = JsonSerializer.Serialize(new { @ref = refBranch, inputs });
        using var response = await httpClient.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
        response.EnsureSuccessStatusCode();

        // workflow_dispatch responde 204 sin id; el run se resuelve después
        // por branch+event vía el reconciler. 0 = "aún sin run id".
        return 0;
    }

    public async Task<(string Status, string Conclusion)> GetRunStatusAsync(long runId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"repos/{owner}/{repository}/actions/runs/{runId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return (doc.RootElement.GetProperty("status").GetString() ?? "unknown",
                doc.RootElement.TryGetProperty("conclusion", out var c) ? c.GetString() ?? "none" : "none");
    }

    public async Task CommentOnIssueAsync(int issueNumber, string body, CancellationToken cancellationToken)
    {
        var body2 = JsonSerializer.Serialize(new { body });
        await httpClient.PostAsync(
            $"repos/{owner}/{repository}/issues/{issueNumber}/comments",
            new StringContent(body2, Encoding.UTF8, "application/json"),
            cancellationToken);
    }
}
