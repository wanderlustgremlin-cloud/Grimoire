using Grimoire.Core.Load;
using Microsoft.Data.SqlClient;

namespace Grimoire.Provider.SqlServer;

public sealed class SqlServerTargetProvider : ITargetProvider
{
    private readonly string _connectionString;

    public SqlServerTargetProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ITargetSession> BeginSessionAsync(string targetTable, CancellationToken ct)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var transaction = connection.BeginTransaction();
        return new SqlServerTargetSession(connection, transaction, targetTable);
    }
}
