namespace Grimoire.Core.Extract.Dialects;

public sealed class OracleDialect : ISqlDialect
{
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    public string QualifyTable(string table, string? schema = null)
        => schema is not null ? $"\"{schema}\".\"{table}\"" : $"\"{table}\"";

    public string BuildPagination(int? limit, int? offset)
        => (offset, limit) switch
        {
            (not null, not null) => $" OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY",
            (null, not null) => $" FETCH FIRST {limit} ROWS ONLY",
            _ => ""
        };
}
