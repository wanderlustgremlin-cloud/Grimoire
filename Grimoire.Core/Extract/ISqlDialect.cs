namespace Grimoire.Core.Extract;

public interface ISqlDialect
{
    string QuoteIdentifier(string identifier);
    string QualifyTable(string table, string? schema = null);
    string BuildPagination(int? limit, int? offset);
}
