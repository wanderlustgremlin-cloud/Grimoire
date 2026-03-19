using Grimoire.Core.Load;
using Oracle.ManagedDataAccess.Client;

namespace Grimoire.Provider.Oracle;

public sealed class OracleTargetProvider : ITargetProvider
{
    private readonly string _connectionString;

    public OracleTargetProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ITargetSession> BeginSessionAsync(string targetTable, CancellationToken ct)
    {
        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);
        var transaction = connection.BeginTransaction();
        return new OracleTargetSession(connection, transaction, targetTable);
    }
}
