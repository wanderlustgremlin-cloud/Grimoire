namespace Grimoire.Observability.SignalR;

public interface IGrimoireHub
{
    Task PipelineStarted();
    Task ProgressUpdated(string entityName, int rowsProcessed);
    Task EntityCompleted(string entityName, EntityCompletedArgs result);
    Task BatchLoaded(BatchLoadedArgs batch);
    Task PipelineCompleted(PipelineCompletedArgs result);
    Task ErrorOccurred(ErrorArgs error);
}

public sealed class BatchLoadedArgs
{
    public required string EntityName { get; init; }
    public required int BatchNumber { get; init; }
    public required int RowsInBatch { get; init; }
    public required int RowsInserted { get; init; }
    public required int RowsUpdated { get; init; }
    public required double DurationMs { get; init; }
    public required double RowsPerSec { get; init; }
    public required int BatchSize { get; init; }
}

public sealed class EntityCompletedArgs
{
    public required int RowsInserted { get; init; }
    public required int RowsUpdated { get; init; }
    public required int RowsErrored { get; init; }
    public required bool Success { get; init; }
    public required TimeSpan Duration { get; init; }
}

public sealed class PipelineCompletedArgs
{
    public required bool Success { get; init; }
    public required int TotalRowsInserted { get; init; }
    public required int TotalRowsUpdated { get; init; }
    public required int TotalRowsErrored { get; init; }
    public required TimeSpan TotalDuration { get; init; }
}

public sealed class ErrorArgs
{
    public required string EntityName { get; init; }
    public required string ErrorType { get; init; }
    public required string Message { get; init; }
}
