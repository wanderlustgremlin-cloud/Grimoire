namespace Grimoire.Core.Extract;

public sealed class TableSchema
{
    public required string TableName { get; init; }
    public string? Schema { get; init; }
    public List<string> Columns { get; init; } = [];
    public List<JoinDefinition> Joins { get; init; } = [];
}

public sealed class JoinDefinition
{
    public required string FromTable { get; init; }
    public required string FromColumn { get; init; }
    public required string ToTable { get; init; }
    public required string ToColumn { get; init; }
}
