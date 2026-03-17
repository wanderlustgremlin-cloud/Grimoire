using System.Collections.Concurrent;

namespace Grimoire.Core.KeyMap;

public sealed class KeyMap
{
    private readonly ConcurrentDictionary<(Type EntityType, object LegacyKey), object> _map = new();

    public void Register<TEntity>(object legacyKey, object newKey)
    {
        _map[(typeof(TEntity), legacyKey)] = newKey;
    }

    public object? Resolve<TEntity>(object legacyKey)
    {
        return _map.TryGetValue((typeof(TEntity), legacyKey), out var newKey) ? newKey : null;
    }

    public bool TryResolve<TEntity>(object legacyKey, out object? newKey)
    {
        return _map.TryGetValue((typeof(TEntity), legacyKey), out newKey);
    }

    internal void Register(Type entityType, object legacyKey, object newKey)
    {
        _map[(entityType, legacyKey)] = newKey;
    }

    internal object? Resolve(Type entityType, object legacyKey)
    {
        return _map.TryGetValue((entityType, legacyKey), out var newKey) ? newKey : null;
    }

    public void Clear() => _map.Clear();
}
