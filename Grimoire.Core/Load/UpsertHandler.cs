using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Grimoire.Core.Load;

internal sealed class UpsertHandler
{
    private readonly MatchConfig _matchConfig;
    private readonly string _targetTable;
    private readonly List<PropertyInfo> _properties;
    private HashSet<string>? _identityColumns;

    public UpsertHandler(MatchConfig matchConfig, string targetTable, List<PropertyInfo> properties)
    {
        _matchConfig = matchConfig;
        _targetTable = targetTable;
        _properties = properties;
    }

    public async Task<(int Inserted, int Updated, int Skipped)> UpsertBatchAsync<TEntity>(
        SqlConnection connection,
        SqlTransaction transaction,
        List<TEntity> batch,
        CancellationToken cancellationToken) where TEntity : class
    {
        var identityColumns = await GetIdentityColumnsAsync(connection, transaction, cancellationToken);

        var matchProps = _properties
            .Where(p => _matchConfig.MatchColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var nonMatchProps = _properties
            .Where(p => !_matchConfig.MatchColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // Properties to write (exclude identity columns)
        var insertProps = _properties
            .Where(p => !identityColumns.Contains(p.Name))
            .ToList();

        var updateProps = nonMatchProps
            .Where(p => !identityColumns.Contains(p.Name))
            .ToList();

        // Identity property for reading back generated values
        var identityProp = _properties
            .FirstOrDefault(p => identityColumns.Contains(p.Name));

        int inserted = 0, updated = 0, skipped = 0;

        foreach (var entity in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existingRow = await FindExistingRowAsync(connection, transaction, matchProps, entity, nonMatchProps, cancellationToken);

            if (existingRow is null)
            {
                await InsertRowAsync(connection, transaction, entity, insertProps, identityProp, cancellationToken);
                inserted++;
            }
            else
            {
                // Set identity value from existing row so key tracking works
                if (identityProp is not null && existingRow.TryGetValue(identityProp.Name, out var existingId) && existingId is not null)
                    identityProp.SetValue(entity, Convert.ChangeType(existingId, identityProp.PropertyType));

                if (_matchConfig.WhenMatchedStrategy == UpdateStrategy.Skip)
                {
                    skipped++;
                }
                else if (_matchConfig.WhenMatchedStrategy == UpdateStrategy.OverwriteChanged)
                {
                    if (HasChanges(entity, existingRow, updateProps))
                    {
                        await UpdateRowAsync(connection, transaction, matchProps, updateProps, entity, cancellationToken);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else // OverwriteAll
                {
                    await UpdateRowAsync(connection, transaction, matchProps, updateProps, entity, cancellationToken);
                    updated++;
                }
            }
        }

        return (inserted, updated, skipped);
    }

    private async Task<HashSet<string>> GetIdentityColumnsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (_identityColumns is not null) return _identityColumns;

        _identityColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var cmd = new SqlCommand(
            "SELECT c.name FROM sys.columns c WHERE c.object_id = OBJECT_ID(@table) AND c.is_identity = 1",
            connection, transaction);
        cmd.Parameters.AddWithValue("@table", _targetTable);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            _identityColumns.Add(reader.GetString(0));
        }

        return _identityColumns;
    }

    private async Task<Dictionary<string, object?>?> FindExistingRowAsync<TEntity>(
        SqlConnection connection,
        SqlTransaction transaction,
        List<PropertyInfo> matchProps,
        TEntity entity,
        List<PropertyInfo> selectProps,
        CancellationToken cancellationToken) where TEntity : class
    {
        var whereClauses = matchProps.Select((p, i) => $"[{p.Name}] = @p{i}");
        var selectColumns = selectProps.Select(p => $"[{p.Name}]");
        var sql = $"SELECT {string.Join(", ", selectColumns)} FROM [{_targetTable}] WHERE {string.Join(" AND ", whereClauses)}";

        await using var cmd = new SqlCommand(sql, connection, transaction);
        for (int i = 0; i < matchProps.Count; i++)
        {
            var value = matchProps[i].GetValue(entity);
            cmd.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
        }

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }
        return row;
    }

    private async Task InsertRowAsync<TEntity>(
        SqlConnection connection,
        SqlTransaction transaction,
        TEntity entity,
        List<PropertyInfo> insertProps,
        PropertyInfo? identityProp,
        CancellationToken cancellationToken) where TEntity : class
    {
        var columns = insertProps.Select(p => $"[{p.Name}]");
        var paramNames = insertProps.Select((_, i) => $"@p{i}");

        string sql;
        if (identityProp is not null)
            sql = $"INSERT INTO [{_targetTable}] ({string.Join(", ", columns)}) OUTPUT INSERTED.[{identityProp.Name}] VALUES ({string.Join(", ", paramNames)})";
        else
            sql = $"INSERT INTO [{_targetTable}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)})";

        await using var cmd = new SqlCommand(sql, connection, transaction);
        for (int i = 0; i < insertProps.Count; i++)
        {
            var value = insertProps[i].GetValue(entity);
            cmd.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
        }

        if (identityProp is not null)
        {
            var generatedId = await cmd.ExecuteScalarAsync(cancellationToken);
            if (generatedId is not null and not DBNull)
                identityProp.SetValue(entity, Convert.ChangeType(generatedId, identityProp.PropertyType));
        }
        else
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task UpdateRowAsync<TEntity>(
        SqlConnection connection,
        SqlTransaction transaction,
        List<PropertyInfo> matchProps,
        List<PropertyInfo> updateProps,
        TEntity entity,
        CancellationToken cancellationToken) where TEntity : class
    {
        var setClauses = updateProps.Select((p, i) => $"[{p.Name}] = @s{i}");
        var whereClauses = matchProps.Select((p, i) => $"[{p.Name}] = @w{i}");
        var sql = $"UPDATE [{_targetTable}] SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";

        await using var cmd = new SqlCommand(sql, connection, transaction);
        for (int i = 0; i < updateProps.Count; i++)
        {
            var value = updateProps[i].GetValue(entity);
            cmd.Parameters.AddWithValue($"@s{i}", value ?? DBNull.Value);
        }
        for (int i = 0; i < matchProps.Count; i++)
        {
            var value = matchProps[i].GetValue(entity);
            cmd.Parameters.AddWithValue($"@w{i}", value ?? DBNull.Value);
        }

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool HasChanges<TEntity>(TEntity entity, Dictionary<string, object?> existingRow, List<PropertyInfo> props) where TEntity : class
    {
        foreach (var prop in props)
        {
            var newValue = prop.GetValue(entity);
            existingRow.TryGetValue(prop.Name, out var oldValue);

            if (newValue is null && oldValue is null) continue;
            if (newValue is null || oldValue is null) return true;
            if (!newValue.Equals(oldValue)) return true;
        }
        return false;
    }
}
