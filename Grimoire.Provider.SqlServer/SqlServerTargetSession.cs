using System.Data;
using Grimoire.Core.Load;
using Microsoft.Data.SqlClient;

namespace Grimoire.Provider.SqlServer;

public sealed class SqlServerTargetSession : ITargetSession
{
    private readonly SqlConnection _connection;
    private readonly SqlTransaction _transaction;
    private readonly string _targetTable;

    internal SqlServerTargetSession(SqlConnection connection, SqlTransaction transaction, string targetTable)
    {
        _connection = connection;
        _transaction = transaction;
        _targetTable = targetTable;
    }

    public async Task<HashSet<string>> GetGeneratedColumnsAsync(CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var cmd = new SqlCommand(
            "SELECT c.name FROM sys.columns c WHERE c.object_id = OBJECT_ID(@table) AND c.is_identity = 1",
            _connection, _transaction);
        cmd.Parameters.AddWithValue("@table", _targetTable);

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
        var table = new DataTable();
        foreach (var col in columns)
        {
            table.Columns.Add(col, typeof(object));
        }

        foreach (var row in rows)
        {
            var dataRow = table.NewRow();
            foreach (var col in columns)
            {
                dataRow[col] = row.TryGetValue(col, out var value) ? value ?? DBNull.Value : DBNull.Value;
            }
            table.Rows.Add(dataRow);
        }

        using var bulkCopy = new SqlBulkCopy(_connection, SqlBulkCopyOptions.Default, _transaction)
        {
            DestinationTableName = $"[{_targetTable}]",
            BatchSize = batchSize
        };

        foreach (var col in columns)
        {
            bulkCopy.ColumnMappings.Add(col, col);
        }

        await bulkCopy.WriteToServerAsync(table, ct);
    }

    public async Task<Dictionary<string, object?>?> FindRowAsync(
        IReadOnlyList<(string Column, object? Value)> matchValues,
        IReadOnlyList<string> selectColumns,
        CancellationToken ct)
    {
        var selectClause = string.Join(", ", selectColumns.Select(c => $"[{c}]"));
        var sql = $"SELECT {selectClause} FROM [{_targetTable}] WHERE {BuildWhereClause(matchValues, "p")}";

        await using var cmd = new SqlCommand(sql, _connection, _transaction);
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
        var columns = string.Join(", ", values.Select(v => $"[{v.Column}]"));
        var paramNames = string.Join(", ", values.Select((_, i) => $"@p{i}"));

        string sql;
        if (generatedColumn is not null)
            sql = $"INSERT INTO [{_targetTable}] ({columns}) OUTPUT INSERTED.[{generatedColumn}] VALUES ({paramNames})";
        else
            sql = $"INSERT INTO [{_targetTable}] ({columns}) VALUES ({paramNames})";

        await using var cmd = new SqlCommand(sql, _connection, _transaction);
        for (int i = 0; i < values.Count; i++)
        {
            cmd.Parameters.AddWithValue($"@p{i}", values[i].Value ?? DBNull.Value);
        }

        if (generatedColumn is not null)
        {
            var result = await cmd.ExecuteScalarAsync(ct);
            return result is DBNull ? null : result;
        }

        await cmd.ExecuteNonQueryAsync(ct);
        return null;
    }

    public async Task UpdateRowAsync(
        IReadOnlyList<(string Column, object? Value)> setValues,
        IReadOnlyList<(string Column, object? Value)> whereValues,
        CancellationToken ct)
    {
        var setClauses = string.Join(", ", setValues.Select((v, i) => $"[{v.Column}] = @s{i}"));
        var sql = $"UPDATE [{_targetTable}] SET {setClauses} WHERE {BuildWhereClause(whereValues, "w")}";

        await using var cmd = new SqlCommand(sql, _connection, _transaction);
        for (int i = 0; i < setValues.Count; i++)
        {
            cmd.Parameters.AddWithValue($"@s{i}", setValues[i].Value ?? DBNull.Value);
        }
        AddWhereParameters(cmd, whereValues, "w");

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<object?> ReadGeneratedKeyAsync(
        string keyColumn,
        IReadOnlyList<(string Column, object? Value)> matchValues,
        CancellationToken ct)
    {
        var sql = $"SELECT [{keyColumn}] FROM [{_targetTable}] WHERE {BuildWhereClause(matchValues, "p")}";

        await using var cmd = new SqlCommand(sql, _connection, _transaction);
        AddWhereParameters(cmd, matchValues, "p");

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null or DBNull ? null : result;
    }

    public async Task CommitAsync(CancellationToken ct)
    {
        await _transaction.CommitAsync(ct);
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        await _transaction.RollbackAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static string BuildWhereClause(IReadOnlyList<(string Column, object? Value)> values, string paramPrefix)
    {
        var clauses = values.Select((v, i) =>
            v.Value is null ? $"[{v.Column}] IS NULL" : $"[{v.Column}] = @{paramPrefix}{i}");
        return string.Join(" AND ", clauses);
    }

    private static void AddWhereParameters(SqlCommand cmd, IReadOnlyList<(string Column, object? Value)> values, string paramPrefix)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i].Value is not null)
                cmd.Parameters.AddWithValue($"@{paramPrefix}{i}", values[i].Value);
        }
    }
}
