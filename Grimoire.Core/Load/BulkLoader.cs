using System.Diagnostics;
using System.Reflection;
using Grimoire.Core.Results;

namespace Grimoire.Core.Load;

internal sealed class BulkLoader
{
    private readonly ITargetProvider _provider;
    private readonly string _targetTable;
    private readonly int _batchSize;
    private readonly MatchConfig? _matchConfig;
    private readonly KeyMap.KeyMap _keyMap;
    private readonly Type _entityType;
    private readonly string? _trackKeyProperty;
    private readonly string? _trackKeyLegacyColumn;
    private readonly bool _trackKeyAppGenerated;
    private readonly bool _trackKeyDbGenerated;

    public BulkLoader(
        ITargetProvider provider,
        string targetTable,
        int batchSize,
        MatchConfig? matchConfig,
        KeyMap.KeyMap keyMap,
        Type entityType,
        string? trackKeyProperty,
        string? trackKeyLegacyColumn,
        bool trackKeyAppGenerated = false,
        bool trackKeyDbGenerated = false)
    {
        _provider = provider;
        _targetTable = targetTable;
        _batchSize = batchSize;
        _matchConfig = matchConfig;
        _keyMap = keyMap;
        _entityType = entityType;
        _trackKeyProperty = trackKeyProperty;
        _trackKeyLegacyColumn = trackKeyLegacyColumn;
        _trackKeyAppGenerated = trackKeyAppGenerated;
        _trackKeyDbGenerated = trackKeyDbGenerated;
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

        await using var session = await _provider.BeginSessionAsync(_targetTable, cancellationToken);

        try
        {
            var generatedColumns = await session.GetGeneratedColumnsAsync(cancellationToken);

            if (_trackKeyDbGenerated && _trackKeyProperty is not null)
                generatedColumns.Add(_trackKeyProperty);

            if (_trackKeyAppGenerated && _trackKeyProperty is not null)
                generatedColumns.Remove(_trackKeyProperty);

            if (_matchConfig is not null && _matchConfig.MatchColumns.Count > 0)
            {
                await LoadWithUpsertAsync(entities, session, properties, generatedColumns, result, onRowError, onProgress, onBatchLoaded, cancellationToken);
            }
            else
            {
                var insertProperties = properties
                    .Where(p => !generatedColumns.Contains(p.Name))
                    .ToList();
                await LoadWithBulkCopyAsync(entities, session, properties, insertProperties, result, onRowError, onProgress, onBatchLoaded, cancellationToken);
            }

            await session.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await session.RollbackAsync(cancellationToken);
            result.Errors.Add(new RowError(entityName, RowErrorType.LoadError, $"Load failed: {ex.Message}", Exception: ex));
        }

        return result;
    }

    private async Task LoadWithBulkCopyAsync<TEntity>(
        IAsyncEnumerable<(TEntity Entity, object? LegacyKey)> entities,
        ITargetSession session,
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
            if (batch.Count >= _batchSize)
            {
                batchNumber++;
                var insertedBefore = result.RowsInserted;
                var sw = Stopwatch.StartNew();
                await FlushBulkCopyBatchAsync(batch, session, properties, insertProperties, result, cancellationToken);
                sw.Stop();
                onBatchLoaded?.Invoke(new BatchResult(
                    result.EntityName, batchNumber, batch.Count,
                    result.RowsInserted - insertedBefore, 0, 0,
                    sw.Elapsed, _batchSize));
                onProgress?.Invoke(result.RowsInserted);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            batchNumber++;
            var insertedBefore = result.RowsInserted;
            var sw = Stopwatch.StartNew();
            await FlushBulkCopyBatchAsync(batch, session, properties, insertProperties, result, cancellationToken);
            sw.Stop();
            onBatchLoaded?.Invoke(new BatchResult(
                result.EntityName, batchNumber, batch.Count,
                result.RowsInserted - insertedBefore, 0, 0,
                sw.Elapsed, _batchSize));
            onProgress?.Invoke(result.RowsInserted);
        }
    }

    private async Task FlushBulkCopyBatchAsync<TEntity>(
        List<(TEntity Entity, object? LegacyKey)> batch,
        ITargetSession session,
        List<PropertyInfo> allProperties,
        List<PropertyInfo> insertProperties,
        EntityResult result,
        CancellationToken cancellationToken) where TEntity : class, new()
    {
        var columns = insertProperties.Select(p => p.Name).ToList();
        var rows = new List<Dictionary<string, object?>>(batch.Count);

        foreach (var (entity, _) in batch)
        {
            var row = new Dictionary<string, object?>();
            foreach (var prop in insertProperties)
            {
                row[prop.Name] = prop.GetValue(entity);
            }
            rows.Add(row);
        }

        await session.BulkInsertAsync(rows, columns, _batchSize, cancellationToken);
        result.RowsInserted += batch.Count;

        if (_trackKeyProperty is not null)
        {
            await TrackInsertedKeysAsync(batch, session, allProperties, cancellationToken);
        }
    }

    private async Task TrackInsertedKeysAsync<TEntity>(
        List<(TEntity Entity, object? LegacyKey)> batch,
        ITargetSession session,
        List<PropertyInfo> properties,
        CancellationToken cancellationToken) where TEntity : class, new()
    {
        if (_trackKeyProperty is null || _trackKeyLegacyColumn is null) return;

        var keyProp = properties.FirstOrDefault(p => p.Name.Equals(_trackKeyProperty, StringComparison.OrdinalIgnoreCase));
        if (keyProp is null) return;

        if (_matchConfig is { MatchColumns.Count: > 0 })
        {
            var matchProps = properties
                .Where(p => _matchConfig.MatchColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var (entity, legacyKey) in batch)
            {
                if (legacyKey is null) continue;

                var matchValues = matchProps
                    .Select(p => (p.Name, p.GetValue(entity)))
                    .ToList();

                var newKey = await session.ReadGeneratedKeyAsync(_trackKeyProperty, matchValues, cancellationToken);
                if (newKey is not null)
                {
                    _keyMap.Register(_entityType, legacyKey, newKey);
                }
            }
        }
        else
        {
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
        ITargetSession session,
        List<PropertyInfo> properties,
        HashSet<string> generatedColumns,
        EntityResult result,
        Action<RowError>? onRowError,
        Action<int>? onProgress,
        Action<BatchResult>? onBatchLoaded,
        CancellationToken cancellationToken) where TEntity : class, new()
    {
        var handler = new UpsertHandler(_matchConfig!, session, properties, generatedColumns);
        var batch = new List<TEntity>();
        var batchKeys = new List<object?>();
        var batchNumber = 0;

        await foreach (var (entity, legacyKey) in entities.WithCancellation(cancellationToken))
        {
            batch.Add(entity);
            batchKeys.Add(legacyKey);

            if (batch.Count >= _batchSize)
            {
                batchNumber++;
                var sw = Stopwatch.StartNew();
                var (ins, upd, skip) = await handler.UpsertBatchAsync(batch, cancellationToken);
                sw.Stop();
                result.RowsInserted += ins;
                result.RowsUpdated += upd;
                result.RowsSkipped += skip;

                if (_trackKeyProperty is not null)
                    TrackUpsertedKeys(batch, batchKeys, properties);

                onBatchLoaded?.Invoke(new BatchResult(
                    result.EntityName, batchNumber, batch.Count,
                    ins, upd, skip, sw.Elapsed, _batchSize));
                onProgress?.Invoke(result.RowsInserted + result.RowsUpdated);
                batch.Clear();
                batchKeys.Clear();
            }
        }

        if (batch.Count > 0)
        {
            batchNumber++;
            var sw = Stopwatch.StartNew();
            var (ins, upd, skip) = await handler.UpsertBatchAsync(batch, cancellationToken);
            sw.Stop();
            result.RowsInserted += ins;
            result.RowsUpdated += upd;
            result.RowsSkipped += skip;

            if (_trackKeyProperty is not null)
                TrackUpsertedKeys(batch, batchKeys, properties);

            onBatchLoaded?.Invoke(new BatchResult(
                result.EntityName, batchNumber, batch.Count,
                ins, upd, skip, sw.Elapsed, _batchSize));
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
