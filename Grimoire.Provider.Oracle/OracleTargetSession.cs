using System.Text;
using Grimoire.Core.Load;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Grimoire.Provider.Oracle;

public sealed class OracleTargetSession : ITargetSession
{
    private readonly OracleConnection _connection;
    private readonly OracleTransaction _transaction;
    private readonly string _targetTable;

    internal OracleTargetSession(OracleConnection connection, OracleTransaction transaction, string targetTable)
    {
        _connection = connection;
        _transaction = transaction;
        _targetTable = targetTable;
    }

    public async Task<HashSet<string>> GetGeneratedColumnsAsync(CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var cmd = new OracleCommand(
            "SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = :table AND IDENTITY_COLUMN = 'YES'",
            _connection);
        cmd.Transaction = _transaction;
        cmd.Parameters.Add(new OracleParameter(":table", _targetTable.ToUpperInvariant()));

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    public async Task BulkInsertAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<string> columns,
        int batchSize,
        CancellationToken ct)
    {
        if (rows.Count == 0) return;

        // Oracle INSERT ALL supports up to ~1000 rows per statement
        const int maxRowsPerBatch = 1000;

        for (int offset = 0; offset < rows.Count; offset += maxRowsPerBatch)
        {
            var batchRows = rows.Skip(offset).Take(maxRowsPerBatch).ToList();
            await InsertAllBatchAsync(batchRows, columns, ct);
        }
    }

    private async Task InsertAllBatchAsync(
        List<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<string> columns,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.Append("INSERT ALL");

        await using var cmd = new OracleCommand();
        cmd.Connection = _connection;
        cmd.Transaction = _transaction;

        var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));

        for (int r = 0; r < rows.Count; r++)
        {
            sb.Append($" INTO \"{_targetTable}\" ({columnList}) VALUES (");
            for (int c = 0; c < columns.Count; c++)
            {
                if (c > 0) sb.Append(", ");
                var paramName = $":r{r}_c{c}";
                sb.Append(paramName);
                var value = rows[r].TryGetValue(columns[c], out var v) ? v ?? DBNull.Value : DBNull.Value;
                cmd.Parameters.Add(new OracleParameter(paramName, value));
            }
            sb.Append(')');
        }

        sb.Append(" SELECT 1 FROM DUAL");
        cmd.CommandText = sb.ToString();
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<Dictionary<string, object?>?> FindRowAsync(
        IReadOnlyList<(string Column, object? Value)> matchValues,
        IReadOnlyList<string> selectColumns,
        CancellationToken ct)
    {
        var selectClause = string.Join(", ", selectColumns.Select(c => $"\"{c}\""));
        var sql = $"SELECT {selectClause} FROM \"{_targetTable}\" WHERE {BuildWhereClause(matchValues, "p")}";

        await using var cmd = new OracleCommand(sql, _connection);
        cmd.Transaction = _transaction;
        AddWhereParameters(cmd, matchValues, "p");

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }
        return row;
    }

    public async Task<object?> InsertRowAsync(
        IReadOnlyList<(string Column, object? Value)> values,
        string? generatedColumn,
        CancellationToken ct)
    {
        var columnList = string.Join(", ", values.Select(v => $"\"{v.Column}\""));
        var paramNames = string.Join(", ", values.Select((_, i) => $":p{i}"));

        await using var cmd = new OracleCommand();
        cmd.Connection = _connection;
        cmd.Transaction = _transaction;

        if (generatedColumn is not null)
        {
            cmd.CommandText = $"INSERT INTO \"{_targetTable}\" ({columnList}) VALUES ({paramNames}) RETURNING \"{generatedColumn}\" INTO :genId";
        }
        else
        {
            cmd.CommandText = $"INSERT INTO \"{_targetTable}\" ({columnList}) VALUES ({paramNames})";
        }

        for (int i = 0; i < values.Count; i++)
        {
            cmd.Parameters.Add(new OracleParameter($":p{i}", values[i].Value ?? DBNull.Value));
        }

        if (generatedColumn is not null)
        {
            var outParam = new OracleParameter(":genId", OracleDbType.Decimal)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);
            await cmd.ExecuteNonQueryAsync(ct);

            return outParam.Value switch
            {
                OracleDecimal od when !od.IsNull => od.Value,
                DBNull => null,
                null => null,
                _ => outParam.Value
            };
        }

        await cmd.ExecuteNonQueryAsync(ct);
        return null;
    }

    public async Task UpdateRowAsync(
        IReadOnlyList<(string Column, object? Value)> setValues,
        IReadOnlyList<(string Column, object? Value)> whereValues,
        CancellationToken ct)
    {
        var setClauses = string.Join(", ", setValues.Select((v, i) => $"\"{v.Column}\" = :s{i}"));
        var sql = $"UPDATE \"{_targetTable}\" SET {setClauses} WHERE {BuildWhereClause(whereValues, "w")}";

        await using var cmd = new OracleCommand(sql, _connection);
        cmd.Transaction = _transaction;

        for (int i = 0; i < setValues.Count; i++)
        {
            cmd.Parameters.Add(new OracleParameter($":s{i}", setValues[i].Value ?? DBNull.Value));
        }
        AddWhereParameters(cmd, whereValues, "w");

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<object?> ReadGeneratedKeyAsync(
        string keyColumn,
        IReadOnlyList<(string Column, object? Value)> matchValues,
        CancellationToken ct)
    {
        var sql = $"SELECT \"{keyColumn}\" FROM \"{_targetTable}\" WHERE {BuildWhereClause(matchValues, "p")}";

        await using var cmd = new OracleCommand(sql, _connection);
        cmd.Transaction = _transaction;
        AddWhereParameters(cmd, matchValues, "p");

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null or DBNull ? null : result;
    }

    public async Task CommitAsync(CancellationToken ct)
    {
        _transaction.Commit();
        await Task.CompletedTask;
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        _transaction.Rollback();
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _transaction.Dispose();
        await _connection.DisposeAsync();
    }

    private static string BuildWhereClause(IReadOnlyList<(string Column, object? Value)> values, string paramPrefix)
    {
        var clauses = values.Select((v, i) =>
            v.Value is null ? $"\"{v.Column}\" IS NULL" : $"\"{v.Column}\" = :{paramPrefix}{i}");
        return string.Join(" AND ", clauses);
    }

    private static void AddWhereParameters(OracleCommand cmd, IReadOnlyList<(string Column, object? Value)> values, string paramPrefix)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i].Value is not null)
                cmd.Parameters.Add(new OracleParameter($":{paramPrefix}{i}", values[i].Value));
        }
    }
}
