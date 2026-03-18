using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Grimoire.Core.Load;

internal sealed class UpsertHandler
{
    private readonly MatchConfig _matchConfig;
    private readonly string _targetTable;
    private readonly List<PropertyInfo> _properties;
    private readonly HashSet<string> _generatedColumns;

    public UpsertHandler(MatchConfig matchConfig, string targetTable, List<PropertyInfo> properties, HashSet<string> generatedColumns)
    {
        _matchConfig = matchConfig;
        _targetTable = targetTable;
        _properties = properties;
        _generatedColumns = generatedColumns;
    }

    public async Task<(int Inserted, int Updated, int Skipped)> UpsertBatchAsync<TEntity>(
        SqlConnection connection,
        SqlTransaction transaction,
        List<TEntity> batch,
        CancellationToken cancellationToken) where TEntity : class
    {
        var matchProps = _properties
            .Where(p => _matchConfig.MatchColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var nonMatchProps = _properties
            .Where(p => !_matchConfig.MatchColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // Properties to write (exclude generated columns)
        var insertProps = _properties
            .Where(p => !_generatedColumns.Contains(p.Name))
            .ToList();

        var updateProps = nonMatchProps
            .Where(p => !_generatedColumns.Contains(p.Name))
            .ToList();

        // Generated property for reading back DB-generated values (identity or DEFAULT)
        var generatedProp = _properties
            .FirstOrDefault(p => _generatedColumns.Contains(p.Name));

        int inserted = 0, updated = 0, skipped = 0;

        foreach (var entity in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existingRow = await FindExistingRowAsync(connection, transaction, matchProps, entity, nonMatchProps, cancellationToken);

            if (existingRow is null)
            {
                await InsertRowAsync(connection, transaction, entity, insertProps, generatedProp, cancellationToken);
                inserted++;
            }
            else
            {
                // Set identity value from existing row so key tracking works
                if (generatedProp is not null && existingRow.TryGetValue(generatedProp.Name, out var existingId) && existingId is not null)
                    generatedProp.SetValue(entity, Convert.ChangeType(existingId, generatedProp.PropertyType));

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
        PropertyInfo? generatedProp,
        CancellationToken cancellationToken) where TEntity : class
    {
        var columns = insertProps.Select(p => $"[{p.Name}]");
        var paramNames = insertProps.Select((_, i) => $"@p{i}");

        string sql;
        if (generatedProp is not null)
            sql = $"INSERT INTO [{_targetTable}] ({string.Join(", ", columns)}) OUTPUT INSERTED.[{generatedProp.Name}] VALUES ({string.Join(", ", paramNames)})";
        else
            sql = $"INSERT INTO [{_targetTable}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)})";

        await using var cmd = new SqlCommand(sql, connection, transaction);
        for (int i = 0; i < insertProps.Count; i++)
        {
            var value = insertProps[i].GetValue(entity);
            cmd.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
        }

        if (generatedProp is not null)
        {
            var generatedId = await cmd.ExecuteScalarAsync(cancellationToken);
            if (generatedId is not null and not DBNull)
                generatedProp.SetValue(entity, Convert.ChangeType(generatedId, generatedProp.PropertyType));
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
