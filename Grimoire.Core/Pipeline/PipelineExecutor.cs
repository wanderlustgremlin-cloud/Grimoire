using Grimoire.Core.Extract;
using Grimoire.Core.Load;
using Grimoire.Core.Results;

namespace Grimoire.Core.Pipeline;

internal sealed class PipelineExecutor
{
    private readonly IConnector? _connector;
    private readonly ITargetProvider? _defaultTargetProvider;
    private readonly List<EntityRegistration> _entities;
    private readonly PipelineEvents _events;
    private readonly List<IPipelineObserver> _observers;
    private readonly KeyMap.KeyMap _keyMap;

    public PipelineExecutor(
        IConnector? connector,
        ITargetProvider? defaultTargetProvider,
        List<EntityRegistration> entities,
        PipelineEvents events,
        List<IPipelineObserver> observers,
        KeyMap.KeyMap keyMap)
    {
        _connector = connector;
        _defaultTargetProvider = defaultTargetProvider;
        _entities = entities;
        _events = events;
        _observers = observers;
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
            if (entity.LoadConfig.Provider is null && _defaultTargetProvider is null)
                throw new InvalidOperationException($"Entity '{entity.EntityName}' has no target provider. Install a provider package (e.g., Grimoire.Provider.SqlServer) and call .LoadWith(provider) on the pipeline or pass a provider to .LoadInto().");
        }

        // Topological sort
        var sorted = TopologicalSorter.Sort(_entities);

        foreach (var observer in _observers)
            observer.OnPipelineStarted();

        // Execute sequentially in dependency order
        foreach (var entity in sorted)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var observer in _observers)
                observer.OnEntityStarted(entity.EntityName);

            var entityResult = await entity.ExecuteAsync(_connector, _defaultTargetProvider, _keyMap, _events, _observers, cancellationToken);
            result.EntityResults.Add(entityResult);
            _events.OnEntityComplete?.Invoke(entityResult);

            foreach (var observer in _observers)
                observer.OnEntityComplete(entityResult);
        }

        result.TotalDuration = DateTime.UtcNow - startTime;

        foreach (var observer in _observers)
            observer.OnPipelineComplete(result);

        return result;
    }
}
