namespace Grimoire.Core.Extract.Dialects;

public sealed class MySqlDialect : ISqlDialect
{
    public string QuoteIdentifier(string identifier) => $"`{identifier}`";

    public string QualifyTable(string table, string? schema = null)
        => schema is not null ? $"`{schema}`.`{table}`" : $"`{table}`";

    public string BuildPagination(int? limit, int? offset)
        => (offset, limit) switch
        {
            (not null, not null) => $" LIMIT {limit} OFFSET {offset}",
            (null, not null) => $" LIMIT {limit}",
            (not null, null) => $" LIMIT 18446744073709551615 OFFSET {offset}",
            _ => ""
        };
}
