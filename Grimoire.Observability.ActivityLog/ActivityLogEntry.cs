namespace Grimoire.Observability.ActivityLog;

public sealed record ActivityLogEntry
{
    public required Guid RunId { get; init; }
    public required string EntityName { get; init; }
    public required DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int RowsErrored { get; set; }
    public required string Status { get; set; }
}
