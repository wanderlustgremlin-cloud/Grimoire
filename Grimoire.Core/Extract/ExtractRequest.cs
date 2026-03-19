namespace Grimoire.Core.Extract;

public sealed class ExtractRequest
{
    public required string EntityName { get; init; }
    public List<string> SourceTables { get; init; } = [];
    public List<string> SourceColumns { get; init; } = [];
    public int? Limit { get; init; }
    public int? Offset { get; init; }
}
