namespace Grimoire.Core.Pipeline;

internal static class TopologicalSorter
{
    public static List<EntityRegistration> Sort(List<EntityRegistration> entities)
    {
        var lookup = entities.ToDictionary(e => e.EntityType);
        var inDegree = entities.ToDictionary(e => e.EntityType, _ => 0);
        var adjacency = entities.ToDictionary(e => e.EntityType, _ => new List<Type>());

        foreach (var entity in entities)
        {
            foreach (var dep in entity.Dependencies)
            {
                if (!lookup.ContainsKey(dep))
                    throw new InvalidOperationException(
                        $"Entity '{entity.EntityName}' depends on '{dep.Name}', which is not registered in the pipeline.");

                adjacency[dep].Add(entity.EntityType);
                inDegree[entity.EntityType]++;
            }
        }

        var queue = new Queue<Type>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<EntityRegistration>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(lookup[current]);

            foreach (var dependent in adjacency[current])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                    queue.Enqueue(dependent);
            }
        }

        if (sorted.Count != entities.Count)
            throw new InvalidOperationException("Circular dependency detected in entity pipeline.");

        return sorted;
    }
}
