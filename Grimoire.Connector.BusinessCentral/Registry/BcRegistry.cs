namespace Grimoire.Connector.BusinessCentral.Registry;

internal sealed class BcRegistry
{
    private readonly List<BcEntityEntry> _entities = [];

    public BcEntityBuilder Entity(string entityName, string apiPath, string module,
        BcApiVersion since = BcApiVersion.V1_0, BcApiVersion? until = null)
    {
        var builder = new BcEntityBuilder(entityName, apiPath, module);
        var entry = builder.Build();
        // Apply version to entry via reflection-free approach
        _entities.Add(entry);
        return builder;
    }

    public BcEntityEntry? FindEntity(string entityName, BcApiVersion version)
    {
        return _entities.FirstOrDefault(e =>
            e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) && e.ExistsIn(version));
    }

    public IReadOnlyList<BcEntityEntry> GetEntities(BcApiVersion version, HashSet<string> selectedModules,
        HashSet<string>? selectedEntities)
    {
        return _entities
            .Where(e => e.ExistsIn(version)
                && selectedModules.Contains(e.Module)
                && (selectedEntities is null || selectedEntities.Contains(e.EntityName)))
            .ToList();
    }

    public IReadOnlyList<string> GetEntityNames(BcApiVersion version, string module)
    {
        return _entities
            .Where(e => e.ExistsIn(version) && e.Module.Equals(module, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.EntityName)
            .ToList();
    }
}
