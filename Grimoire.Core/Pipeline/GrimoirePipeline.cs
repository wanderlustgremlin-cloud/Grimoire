using Grimoire.Core.Extract;
using Grimoire.Core.Results;

namespace Grimoire.Core.Pipeline;

public sealed class GrimoirePipeline
{
    private IConnector? _connector;
    private readonly List<EntityRegistration> _entities = [];
    private readonly PipelineEvents _events = new();
    private readonly KeyMap.KeyMap _keyMap = new();

    public GrimoirePipeline ExtractFrom(IConnector connector)
    {
        _connector = connector;
        return this;
    }

    public EntityBuilder<TEntity> Entity<TEntity>() where TEntity : class, new()
    {
        var builder = new EntityBuilder<TEntity>(this);
        _entities.Add(builder.Registration);
        return builder;
    }

    public GrimoirePipeline OnRowError(Action<RowError> handler)
    {
        _events.OnRowError = handler;
        return this;
    }

    public GrimoirePipeline OnProgress(Action<string, int> handler)
    {
        _events.OnProgress = handler;
        return this;
    }

    public GrimoirePipeline OnEntityComplete(Action<EntityResult> handler)
    {
        _events.OnEntityComplete = handler;
        return this;
    }

    public async Task<EtlResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var executor = new PipelineExecutor(_connector, _entities, _events, _keyMap);
        return await executor.ExecuteAsync(cancellationToken);
    }
}
