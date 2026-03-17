using Grimoire.Core.Extract;
using Grimoire.Core.Load;
using Grimoire.Core.Results;
using Grimoire.Core.Transform;

namespace Grimoire.Core.Pipeline;

internal sealed class EntityRegistration
{
    public required Type EntityType { get; init; }
    public required string EntityName { get; init; }
    public object? Mapping { get; set; } // GrimoireMapping<T>
    public LoadConfig? LoadConfig { get; set; }
    public MatchConfig? MatchConfig { get; set; }
    public ICustomExtractor? CustomExtractor { get; set; }
    public List<Type> Dependencies { get; } = [];
    public string? TrackKeyProperty { get; set; }
    public string? TrackKeyLegacyColumn { get; set; }

    public async Task<EntityResult> ExecuteAsync(
        IConnector? connector,
        KeyMap.KeyMap keyMap,
        PipelineEvents events,
        List<IPipelineObserver> observers,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        // Use reflection to call the generic ExecuteInternalAsync
        var method = typeof(EntityRegistration)
            .GetMethod(nameof(ExecuteInternalAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(EntityType);

        var result = await (Task<EntityResult>)method.Invoke(this, [connector, keyMap, events, observers, cancellationToken])!;
        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    private async Task<EntityResult> ExecuteInternalAsync<TEntity>(
        IConnector? connector,
        KeyMap.KeyMap keyMap,
        PipelineEvents events,
        List<IPipelineObserver> observers,
        CancellationToken cancellationToken) where TEntity : class, new()
    {
        var result = new EntityResult { EntityName = EntityName };

        if (LoadConfig is null)
            throw new InvalidOperationException($"Entity '{EntityName}' has no load configuration. Call LoadInto() to configure.");

        // Build mapping
        var mapping = (GrimoireMapping<TEntity>?)Mapping
            ?? throw new InvalidOperationException($"Entity '{EntityName}' has no mapping. Call TransformUsing<T>() to configure.");
        var built = mapping.Build();

        var executor = new MappingExecutor<TEntity>(built.Mappings, keyMap, EntityName);

        // Extract
        IAsyncEnumerable<SourceRow> sourceRows;
        if (CustomExtractor is not null)
        {
            var request = new ExtractRequest
            {
                EntityName = EntityName,
                SourceTables = built.SourceTables,
                SourceColumns = built.Mappings.Select(m => m.SourceColumn).ToList()
            };
            sourceRows = CustomExtractor.ExtractAsync(request, cancellationToken);
        }
        else if (connector is not null)
        {
            var extractorBridge = new ConnectorExtractor(connector);
            var request = new ExtractRequest
            {
                EntityName = EntityName,
                SourceTables = built.SourceTables,
                SourceColumns = built.Mappings.Select(m => m.SourceColumn).ToList()
            };
            sourceRows = extractorBridge.ExtractAsync(request, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"Entity '{EntityName}' has no data source. Provide a connector or custom extractor.");
        }

        // Transform + Load (streaming)
        var loader = new BulkLoader(LoadConfig, MatchConfig, keyMap, EntityType, TrackKeyProperty, TrackKeyLegacyColumn);

        async IAsyncEnumerable<(TEntity Entity, object? LegacyKey)> TransformStream()
        {
            await foreach (var row in sourceRows.WithCancellation(cancellationToken))
            {
                result.RowsExtracted++;
                var (entity, error) = executor.Transform(row);

                if (error is not null)
                {
                    result.Errors.Add(error);
                    events.OnRowError?.Invoke(error);
                    foreach (var observer in observers)
                        observer.OnRowError(error);
                    continue;
                }

                object? legacyKey = TrackKeyLegacyColumn is not null ? row[TrackKeyLegacyColumn] : null;
                yield return (entity!, legacyKey);
            }
        }

        var loadResult = await loader.LoadAsync<TEntity>(
            TransformStream(),
            EntityName,
            error =>
            {
                result.Errors.Add(error);
                events.OnRowError?.Invoke(error);
                foreach (var observer in observers)
                    observer.OnRowError(error);
            },
            count =>
            {
                events.OnProgress?.Invoke(EntityName, count);
                foreach (var observer in observers)
                    observer.OnProgress(EntityName, count);
            },
            batch =>
            {
                foreach (var observer in observers)
                    observer.OnBatchLoaded(batch);
            },
            cancellationToken);

        result.RowsInserted = loadResult.RowsInserted;
        result.RowsUpdated = loadResult.RowsUpdated;
        result.RowsSkipped = loadResult.RowsSkipped;
        foreach (var error in loadResult.Errors)
        {
            result.Errors.Add(error);
        }

        return result;
    }
}
