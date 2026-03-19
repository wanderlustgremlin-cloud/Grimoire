namespace Grimoire.Core.Extract.Dialects;

public static class DialectFactory
{
    public static ISqlDialect Create(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => new SqlServerDialect(),
        DatabaseProvider.Postgres => new PostgresDialect(),
        DatabaseProvider.Oracle => new OracleDialect(),
        DatabaseProvider.MySql => new MySqlDialect(),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, $"No built-in dialect for provider '{provider}'.")
    };
}
