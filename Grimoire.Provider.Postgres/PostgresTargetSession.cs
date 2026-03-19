using System.Text;
using Grimoire.Core.Load;
using Npgsql;

namespace Grimoire.Provider.Postgres;

public sealed class PostgresTargetSession : ITargetSession
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private readonly string _targetTable;

    internal PostgresTargetSession(NpgsqlConnection connection, NpgsqlTransaction transaction, string targetTable)
    {
        _connection = connection;
        _transaction = transaction;
        _targetTable = targetTable;
    }

    public async Task<HashSet<string>> GetGeneratedColumnsAsync(CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var cmd = new NpgsqlCommand("""
            SELECT column_name
            FROM information_schema.columns
            WHERE table_name = @table
              AND table_schema = 'public'
              AND (is_identity = 'YES' OR column_default LIKE 'nextval%')
            """, _connection, _transaction);
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
        if (rows.Count == 0) return;

        // Postgres handles multi-row INSERT efficiently
        // Sub-batch to stay within parameter limits (~32K params)
        var maxRowsPerBatch = Math.Max(1, 32000 / Math.Max(1, columns.Count));

        for (int offset = 0; offset < rows.Count; offset += maxRowsPerBatch)
        {
            var batchRows = rows.Skip(offset).Take(maxRowsPerBatch).ToList();
            await InsertBatchAsync(batchRows, columns, ct);
        }
    }

    private async Task InsertBatchAsync(
        List<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<string> columns,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
        sb.Append($"INSERT INTO \"{_targetTable}\" ({columnList}) VALUES ");

        var paramIndex = 0;
        await using var cmd = new NpgsqlCommand();
        cmd.Connection = _connection;
        cmd.Transaction = _transaction;

        for (int r = 0; r < rows.Count; r++)
        {
            if (r > 0) sb.Append(", ");
            sb.Append('(');
            for (int c = 0; c < columns.Count; c++)
            {
                if (c > 0) sb.Append(", ");
                var paramName = $"@p{paramIndex}";
                sb.Append(paramName);
                var value = rows[r].TryGetValue(columns[c], out var v) ? v ?? DBNull.Value : DBNull.Value;
                cmd.Parameters.AddWithValue(paramName, value);
                paramIndex++;
            }
            sb.Append(')');
        }

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

        await using var cmd = new NpgsqlCommand(sql, _connection, _transaction);
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
        var paramNames = string.Join(", ", values.Select((_, i) => $"@p{i}"));

        string sql;
        if (generatedColumn is not null)
            sql = $"INSERT INTO \"{_targetTable}\" ({columnList}) VALUES ({paramNames}) RETURNING \"{generatedColumn}\"";
        else
            sql = $"INSERT INTO \"{_targetTable}\" ({columnList}) VALUES ({paramNames})";

        await using var cmd = new NpgsqlCommand(sql, _connection, _transaction);
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
        var setClauses = string.Join(", ", setValues.Select((v, i) => $"\"{v.Column}\" = @s{i}"));
        var sql = $"UPDATE \"{_targetTable}\" SET {setClauses} WHERE {BuildWhereClause(whereValues, "w")}";

        await using var cmd = new NpgsqlCommand(sql, _connection, _transaction);
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
        var sql = $"SELECT \"{keyColumn}\" FROM \"{_targetTable}\" WHERE {BuildWhereClause(matchValues, "p")}";

        await using var cmd = new NpgsqlCommand(sql, _connection, _transaction);
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
            v.Value is null ? $"\"{v.Column}\" IS NULL" : $"\"{v.Column}\" = @{paramPrefix}{i}");
        return string.Join(" AND ", clauses);
    }

    private static void AddWhereParameters(NpgsqlCommand cmd, IReadOnlyList<(string Column, object? Value)> values, string paramPrefix)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i].Value is not null)
                cmd.Parameters.AddWithValue($"@{paramPrefix}{i}", values[i].Value!);
        }
    }
}
