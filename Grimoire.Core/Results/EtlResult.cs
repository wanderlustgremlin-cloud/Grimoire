namespace Grimoire.Core.Results;

public sealed class EtlResult
{
    public List<EntityResult> EntityResults { get; } = [];
    public TimeSpan TotalDuration { get; set; }
    public bool Success => EntityResults.TrueForAll(r => r.Success);

    public int TotalRowsInserted => EntityResults.Sum(r => r.RowsInserted);
    public int TotalRowsUpdated => EntityResults.Sum(r => r.RowsUpdated);
    public int TotalRowsErrored => EntityResults.Sum(r => r.RowsErrored);
}
