using Grimoire.Core.Extract;
using Grimoire.Core.Results;

namespace Grimoire.Core.Pipeline;

internal sealed class PipelineExecutor
{
    private readonly IConnector? _connector;
    private readonly List<EntityRegistration> _entities;
    private readonly PipelineEvents _events;
    private readonly KeyMap.KeyMap _keyMap;

    public PipelineExecutor(
        IConnector? connector,
        List<EntityRegistration> entities,
        PipelineEvents events,
        KeyMap.KeyMap keyMap)
    {
        _connector = connector;
        _entities = entities;
        _events = events;
        _keyMap = keyMap;
    }

    public async Task<EtlResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var result = new EtlResult();
        var startTime = DateTime.UtcNow;

        // Validate
        foreach (var entity in _entities)
        {
            if (entity.LoadConfig is null)
                throw new InvalidOperationException($"Entity '{entity.EntityName}' has no load configuration.");
            if (entity.Mapping is null)
                throw new InvalidOperationException($"Entity '{entity.EntityName}' has no mapping configured.");
            if (entity.CustomExtractor is null && _connector is null)
                throw new InvalidOperationException($"Entity '{entity.EntityName}' has no data source. Provide a connector or custom extractor.");
        }

        // Topological sort
        var sorted = TopologicalSorter.Sort(_entities);

        // Execute sequentially in dependency order
        foreach (var entity in sorted)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entityResult = await entity.ExecuteAsync(_connector, _keyMap, _events, cancellationToken);
            result.EntityResults.Add(entityResult);
            _events.OnEntityComplete?.Invoke(entityResult);
        }

        result.TotalDuration = DateTime.UtcNow - startTime;
        return result;
    }
}
