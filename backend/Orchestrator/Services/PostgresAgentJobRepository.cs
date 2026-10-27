using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Orchestrator.Domain;

namespace Orchestrator.Services;

/// <summary>
/// Persistencia de jobs en PostgreSQL. La cola usa FOR UPDATE SKIP LOCKED
/// (specs/orchestrator/architecture.md, decisión 2).
/// </summary>
public class PostgresAgentJobRepository : IAgentJobRepository
{
    private readonly NpgsqlDataSource dataSource;

    public PostgresAgentJobRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<bool> TryAddAsync(AgentJob job, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO agent_jobs (id, type, state, trigger_type, payload, attempt_count, delivery_id, github_run_id, created_at)
            VALUES (@id, @type, @state, @trigger, @payload::jsonb, @attempt_count, @delivery_id, @github_run_id, @created_at)
            ON CONFLICT (delivery_id) DO NOTHING
            """);
        command.Parameters.AddWithValue("id", job.Id);
        command.Parameters.AddWithValue("type", job.Type.ToString());
        command.Parameters.AddWithValue("state", job.State.ToString());
        command.Parameters.AddWithValue("trigger", job.Trigger.ToString());
        command.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(job.Payload));
        command.Parameters.AddWithValue("attempt_count", job.AttemptCount);
        command.Parameters.AddWithValue("delivery_id", job.DeliveryId);
        command.Parameters.AddWithValue("github_run_id", NpgsqlDbType.Bigint, job.GithubRunId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("created_at", job.CreatedAt.UtcDateTime);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<AgentJob>> TakeDispatchableAsync(int max, CancellationToken cancellationToken)
    {
        var jobs = new List<AgentJob>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT id, type, state, trigger_type, payload, attempt_count, delivery_id, github_run_id, created_at, dispatched_at, completed_at
            FROM agent_jobs
            WHERE state IN ('Pending', 'Retrying')
            ORDER BY created_at
            FOR UPDATE SKIP LOCKED
            LIMIT @max
            """,
            connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            jobs.Add(MapJob(reader));
        }

        await reader.CloseAsync();
        await transaction.CommitAsync(cancellationToken);
        return jobs;
    }

    public async Task<IReadOnlyList<AgentJob>> ListByStateAsync(JobState state, CancellationToken cancellationToken)
    {
        var jobs = new List<AgentJob>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, type, state, trigger_type, payload, attempt_count, delivery_id, github_run_id, created_at, dispatched_at, completed_at
            FROM agent_jobs WHERE state = @state ORDER BY created_at
            """,
            connection);
        command.Parameters.AddWithValue("state", state.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            jobs.Add(MapJob(reader));
        }

        return jobs;
    }

    public async Task<AgentJob?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, type, state, trigger_type, payload, attempt_count, delivery_id, github_run_id, created_at, dispatched_at, completed_at
            FROM agent_jobs WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapJob(reader);
    }

    public async Task UpdateAsync(AgentJob job, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE agent_jobs
            SET state = @state, attempt_count = @attempts, github_run_id = @run_id,
                dispatched_at = @dispatched_at, completed_at = @completed_at
            WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("id", job.Id);
        command.Parameters.AddWithValue("state", job.State.ToString());
        command.Parameters.AddWithValue("attempts", job.AttemptCount);
        command.Parameters.AddWithValue("run_id", NpgsqlDbType.Bigint, job.GithubRunId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("dispatched_at", NpgsqlDbType.TimestampTz, job.DispatchedAt?.UtcDateTime ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("completed_at", NpgsqlDbType.TimestampTz, job.CompletedAt?.UtcDateTime ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static AgentJob MapJob(NpgsqlDataReader reader)
    {
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(4)) ?? new();
        return new AgentJob(
            reader.GetGuid(0),
            Enum.Parse<JobType>(reader.GetString(1)),
            Enum.Parse<JobState>(reader.GetString(2)),
            Enum.Parse<TriggerType>(reader.GetString(3)),
            payload,
            reader.GetInt32(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetInt64(7),
            new DateTimeOffset(reader.GetDateTime(8), TimeSpan.Zero),
            reader.IsDBNull(9) ? null : new DateTimeOffset(reader.GetDateTime(9), TimeSpan.Zero),
            reader.IsDBNull(10) ? null : new DateTimeOffset(reader.GetDateTime(10), TimeSpan.Zero));
    }
}
