namespace Grimoire.Core.Extract.Dialects;

public sealed class PostgresDialect : ISqlDialect
{
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    public string QualifyTable(string table, string? schema = null)
        => schema is not null ? $"\"{schema}\".\"{table}\"" : $"\"{table}\"";

    public string BuildPagination(int? limit, int? offset)
        => (offset, limit) switch
        {
            (not null, not null) => $" LIMIT {limit} OFFSET {offset}",
            (null, not null) => $" LIMIT {limit}",
            (not null, null) => $" OFFSET {offset}",
            _ => ""
        };
}
