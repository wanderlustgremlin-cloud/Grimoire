namespace Grimoire.Observability.Message;

public sealed class PipelineMessage
{
    public required string PipelineName { get; init; }
    public required bool Success { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public required List<EntitySummary> EntitySummaries { get; init; }
    public required List<string> Errors { get; init; }
}

public sealed class EntitySummary
{
    public required string EntityName { get; init; }
    public required int RowsInserted { get; init; }
    public required int RowsUpdated { get; init; }
    public required int RowsErrored { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool Success { get; init; }
}
