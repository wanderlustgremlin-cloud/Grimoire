using Grimoire.Core.Pipeline;
using Grimoire.Core.Results;
using Microsoft.Data.SqlClient;

namespace Grimoire.Observability.ActivityLog;

public sealed class ActivityLogObserver : IPipelineObserver
{
    private readonly string _connectionString;
    private readonly Guid _runId = Guid.NewGuid();
    private readonly Dictionary<string, DateTime> _entityStartTimes = [];

    public ActivityLogObserver(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Guid RunId => _runId;

    public void OnEntityStarted(string entityName)
    {
        _entityStartTimes[entityName] = DateTime.UtcNow;

        var entry = new ActivityLogEntry
        {
            RunId = _runId,
            EntityName = entityName,
            StartedAt = _entityStartTimes[entityName],
            Status = "Running"
        };

        InsertEntry(entry);
    }

    public void OnEntityComplete(EntityResult result)
    {
        _entityStartTimes.TryGetValue(result.EntityName, out var startedAt);

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE [GrimoireActivityLog]
            SET CompletedAt = @CompletedAt,
                RowsInserted = @RowsInserted,
                RowsUpdated = @RowsUpdated,
                RowsErrored = @RowsErrored,
                Status = @Status
            WHERE RunId = @RunId AND EntityName = @EntityName
            """;

        command.Parameters.AddWithValue("@CompletedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@RowsInserted", result.RowsInserted);
        command.Parameters.AddWithValue("@RowsUpdated", result.RowsUpdated);
        command.Parameters.AddWithValue("@RowsErrored", result.RowsErrored);
        command.Parameters.AddWithValue("@Status", result.Success ? "Completed" : "CompletedWithErrors");
        command.Parameters.AddWithValue("@RunId", _runId);
        command.Parameters.AddWithValue("@EntityName", result.EntityName);

        command.ExecuteNonQuery();
    }

    private void InsertEntry(ActivityLogEntry entry)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO [GrimoireActivityLog] (RunId, EntityName, StartedAt, Status)
            VALUES (@RunId, @EntityName, @StartedAt, @Status)
            """;

        command.Parameters.AddWithValue("@RunId", entry.RunId);
        command.Parameters.AddWithValue("@EntityName", entry.EntityName);
        command.Parameters.AddWithValue("@StartedAt", entry.StartedAt);
        command.Parameters.AddWithValue("@Status", entry.Status);

        command.ExecuteNonQuery();
    }
}
