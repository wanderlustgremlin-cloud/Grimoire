namespace Grimoire.Core.Results;

public sealed class EntityResult
{
    public required string EntityName { get; init; }
    public int RowsExtracted { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int RowsSkipped { get; set; }
    public int RowsErrored => Errors.Count;
    public List<RowError> Errors { get; } = [];
    public TimeSpan Duration { get; set; }
    public bool Success => Errors.Count == 0;
}
