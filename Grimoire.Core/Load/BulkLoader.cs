using System.Data;
using System.Diagnostics;
using System.Reflection;
using Grimoire.Core.Results;
using Microsoft.Data.SqlClient;

namespace Grimoire.Core.Load;

internal sealed class BulkLoader
{
    private readonly LoadConfig _loadConfig;
    private readonly MatchConfig? _matchConfig;
    private readonly KeyMap.KeyMap _keyMap;
    private readonly Type _entityType;
    private readonly string? _trackKeyProperty;
    private readonly string? _trackKeyLegacyColumn;

    public BulkLoader(
        LoadConfig loadConfig,
        MatchConfig? matchConfig,
        KeyMap.KeyMap keyMap,
        Type entityType,
        string? trackKeyProperty,
        string? trackKeyLegacyColumn)
    {
        _loadConfig = loadConfig;
        _matchConfig = matchConfig;
        _keyMap = keyMap;
        _entityType = entityType;
        _trackKeyProperty = trackKeyProperty;
        _trackKeyLegacyColumn = trackKeyLegacyColumn;
    }

    public async Task<EntityResult> LoadAsync<TEntity>(
        IAsyncEnumerable<(TEntity Entity, object? LegacyKey)> entities,
        string entityName,
        Action<RowError>? onRowError,
        Action<int>? onProgress,
        Action<BatchResult>? onBatchLoaded,
        CancellationToken cancellationToken) where TEntity : class, new()
    {
        var result = new EntityResult { EntityName = entityName };
        var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToList();

        await using var connection = new SqlConnection(_loadConfig.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction();

        try
        {
            // Detect identity columns to exclude from INSERT statements
            var identityColumns = await GetIdentityColumnsAsync(connection, transaction, cancellationToken);

            if (_matchConfig is not null && _matchConfig.MatchColumns.Count > 0)
            {
                await LoadWithUpsertAsync(entities, connection, transaction, properties, result, onRowError, onProgress, onBatchLoaded, cancellationToken);
            }
            else
            {
                var insertProperties = properties
                    .Where(p => !identityColumns.Contains(p.Name))
                    .ToList();
                await LoadWithBulkCopyAsync(entities, connection, transaction, properties, insertProperties, result, onRowError, onProgress, onBatchLoaded, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            result.Errors.Add(new RowError(entityName, RowErrorType.LoadError, $"Load failed: {ex.Message}", Exception: ex));
        }

        return result;
    }

    private async Task<HashSet<string>> GetIdentityColumnsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var identityColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var cmd = new SqlCommand(
            "SELECT c.name FROM sys.columns c WHERE c.object_id = OBJECT_ID(@table) AND c.is_identity = 1",
            connection, transaction);
        cmd.Parameters.AddWithValue("@table", _loadConfig.TargetTable);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            identityColumns.Add(reader.GetString(0));
        }

        return identityColumns;
    }

    private async Task LoadWithBulkCopyAsync<TEntity>(
        IAsyncEnumerable<(TEntity Entity, object? LegacyKey)> entities,
        SqlConnection connection,
        SqlTransaction transaction,
        List<PropertyInfo> properties,
        List<PropertyInfo> insertProperties,
        EntityResult result,
        Action<RowError>? onRowError,
        Action<int>? onProgress,
        Action<BatchResult>? onBatchLoaded,
        CancellationToken cancellationToken) where TEntity : class, new()
    {
        var batch = new List<(TEntity Entity, object? LegacyKey)>();
        var batchNumber = 0;

        await foreach (var item in entities.WithCancellation(cancellationToken))
        {
            batch.Add(item);
            if (batch.Count >= _loadConfig.BatchSize)
            {
                batchNumber++;
                var insertedBefore = result.RowsInserted;
                var sw = Stopwatch.StartNew();
                await FlushBulkCopyBatchAsync(batch, connection, transaction, properties, insertProperties, result, cancellationToken);
                sw.Stop();
                onBatchLoaded?.Invoke(new BatchResult(
                    result.EntityName, batchNumber, batch.Count,
                    result.RowsInserted - insertedBefore, 0, 0,
                    sw.Elapsed, _loadConfig.BatchSize));
                onProgress?.Invoke(result.RowsInserted);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            batchNumber++;
            var insertedBefore = result.RowsInserted;
            var sw = Stopwatch.StartNew();
            await FlushBulkCopyBatchAsync(batch, connection, transaction, properties, insertProperties, result, cancellationToken);
            sw.Stop();
            onBatchLoaded?.Invoke(new BatchResult(
                result.EntityName, batchNumber, batch.Count,
                result.RowsInserted - insertedBefore, 0, 0,
                sw.Elapsed, _loadConfig.BatchSize));
            onProgress?.Invoke(result.RowsInserted);
        }
    }

    private async Task FlushBulkCopyBatchAsync<TEntity>(
        List<(TEntity Entity, object? LegacyKey)> batch,
        SqlConnection connection,
        SqlTransaction transaction,
        List<PropertyInfo> allProperties,
        List<PropertyInfo> insertProperties,
        EntityResult result,
        CancellationToken cancellationToken) where TEntity : class, new()
    {
        var table = new DataTable();
        foreach (var prop in insertProperties)
        {
            var colType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            table.Columns.Add(prop.Name, colType);
        }

        foreach (var (entity, _) in batch)
        {
            var row = table.NewRow();
            foreach (var prop in insertProperties)
            {
                row[prop.Name] = prop.GetValue(entity) ?? DBNull.Value;
            }
            table.Rows.Add(row);
        }

        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = $"[{_loadConfig.TargetTable}]",
            BatchSize = _loadConfig.BatchSize
        };

        foreach (var prop in insertProperties)
        {
            bulkCopy.ColumnMappings.Add(prop.Name, prop.Name);
        }

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
        result.RowsInserted += batch.Count;

        // Track keys after insert
        if (_trackKeyProperty is not null)
        {
            await TrackInsertedKeysAsync(batch, connection, transaction, allProperties, cancellationToken);
        }
    }

    private async Task TrackInsertedKeysAsync<TEntity>(
        List<(TEntity Entity, object? LegacyKey)> batch,
        SqlConnection connection,
        SqlTransaction transaction,
        List<PropertyInfo> properties,
        CancellationToken cancellationToken) where TEntity : class, new()
    {
        if (_trackKeyProperty is null || _trackKeyLegacyColumn is null) return;

        var keyProp = properties.FirstOrDefault(p => p.Name.Equals(_trackKeyProperty, StringComparison.OrdinalIgnoreCase));
        if (keyProp is null) return;

        // For bulk copy, we need to query back the inserted keys
        // using the match columns or a known unique column
        if (_matchConfig is { MatchColumns.Count: > 0 })
        {
            var matchProps = properties
                .Where(p => _matchConfig.MatchColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var (entity, legacyKey) in batch)
            {
                if (legacyKey is null) continue;

                var whereClauses = matchProps.Select((p, i) => $"[{p.Name}] = @p{i}");
                var sql = $"SELECT [{_trackKeyProperty}] FROM [{_loadConfig.TargetTable}] WHERE {string.Join(" AND ", whereClauses)}";

                await using var cmd = new SqlCommand(sql, connection, transaction);
                for (int i = 0; i < matchProps.Count; i++)
                {
                    var value = matchProps[i].GetValue(entity);
                    cmd.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
                }

                var newKey = await cmd.ExecuteScalarAsync(cancellationToken);
                if (newKey is not null and not DBNull)
                {
                    _keyMap.Register(_entityType, legacyKey, newKey);
                }
            }
        }
        else
        {
            // Without match columns, track using entity property value directly
            foreach (var (entity, legacyKey) in batch)
            {
                if (legacyKey is null) continue;
                var newKey = keyProp.GetValue(entity);
                if (newKey is not null)
                {
                    _keyMap.Register(_entityType, legacyKey, newKey);
                }
            }
        }
    }

    private async Task LoadWithUpsertAsync<TEntity>(
        IAsyncEnumerable<(TEntity Entity, object? LegacyKey)> entities,
        SqlConnection connection,
        SqlTransaction transaction,
        List<PropertyInfo> properties,
        EntityResult result,
        Action<RowError>? onRowError,
        Action<int>? onProgress,
        Action<BatchResult>? onBatchLoaded,
        CancellationToken cancellationToken) where TEntity : class, new()
    {
        var handler = new UpsertHandler(_matchConfig!, _loadConfig.TargetTable, properties);
        var batch = new List<TEntity>();
        var batchKeys = new List<object?>();
        var batchNumber = 0;

        await foreach (var (entity, legacyKey) in entities.WithCancellation(cancellationToken))
        {
            batch.Add(entity);
            batchKeys.Add(legacyKey);

            if (batch.Count >= _loadConfig.BatchSize)
            {
                batchNumber++;
                var sw = Stopwatch.StartNew();
                var (ins, upd, skip) = await handler.UpsertBatchAsync(connection, transaction, batch, cancellationToken);
                sw.Stop();
                result.RowsInserted += ins;
                result.RowsUpdated += upd;
                result.RowsSkipped += skip;

                if (_trackKeyProperty is not null)
                    TrackUpsertedKeys(batch, batchKeys, properties);

                onBatchLoaded?.Invoke(new BatchResult(
                    result.EntityName, batchNumber, batch.Count,
                    ins, upd, skip, sw.Elapsed, _loadConfig.BatchSize));
                onProgress?.Invoke(result.RowsInserted + result.RowsUpdated);
                batch.Clear();
                batchKeys.Clear();
            }
        }

        if (batch.Count > 0)
        {
            batchNumber++;
            var sw = Stopwatch.StartNew();
            var (ins, upd, skip) = await handler.UpsertBatchAsync(connection, transaction, batch, cancellationToken);
            sw.Stop();
            result.RowsInserted += ins;
            result.RowsUpdated += upd;
            result.RowsSkipped += skip;

            if (_trackKeyProperty is not null)
                TrackUpsertedKeys(batch, batchKeys, properties);

            onBatchLoaded?.Invoke(new BatchResult(
                result.EntityName, batchNumber, batch.Count,
                ins, upd, skip, sw.Elapsed, _loadConfig.BatchSize));
            onProgress?.Invoke(result.RowsInserted + result.RowsUpdated);
        }
    }

    private void TrackUpsertedKeys<TEntity>(List<TEntity> batch, List<object?> legacyKeys, List<PropertyInfo> properties) where TEntity : class
    {
        var keyProp = properties.FirstOrDefault(p => p.Name.Equals(_trackKeyProperty, StringComparison.OrdinalIgnoreCase));
        if (keyProp is null) return;

        for (int i = 0; i < batch.Count; i++)
        {
            var legacyKey = legacyKeys[i];
            if (legacyKey is null) continue;
            var newKey = keyProp.GetValue(batch[i]);
            if (newKey is not null)
            {
                _keyMap.Register(_entityType, legacyKey, newKey);
            }
        }
    }
}
