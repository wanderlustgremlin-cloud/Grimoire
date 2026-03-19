using Grimoire.Core.Load;
using Npgsql;

namespace Grimoire.Provider.Postgres;

public sealed class PostgresTargetProvider : ITargetProvider
{
    private readonly string _connectionString;

    public PostgresTargetProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ITargetSession> BeginSessionAsync(string targetTable, CancellationToken ct)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var transaction = await connection.BeginTransactionAsync(ct);
        return new PostgresTargetSession(connection, transaction, targetTable);
    }
}
