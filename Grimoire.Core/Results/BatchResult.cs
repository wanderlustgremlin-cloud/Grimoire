namespace Grimoire.Core.Results;

public sealed record BatchResult(
    string EntityName,
    int BatchNumber,
    int RowsInBatch,
    int RowsInserted,
    int RowsUpdated,
    int RowsSkipped,
    TimeSpan Duration,
    int BatchSize);
