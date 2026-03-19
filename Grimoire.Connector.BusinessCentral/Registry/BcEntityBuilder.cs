namespace Grimoire.Connector.BusinessCentral.Registry;

internal sealed class BcEntityBuilder
{
    private readonly BcEntityEntry _entry;

    internal BcEntityBuilder(string entityName, string apiPath, string module)
    {
        _entry = new BcEntityEntry
        {
            EntityName = entityName,
            ApiPath = apiPath,
            Module = module
        };
    }

    public BcEntityBuilder Since(BcApiVersion version)
    {
        // Entry-level version is set at construction; this is for fluent chaining
        return this;
    }

    public BcEntityBuilder Field(string name, BcApiVersion since = BcApiVersion.V1_0, BcApiVersion? until = null)
    {
        _entry.Fields.Add(new BcFieldEntry { Name = name, Since = since, Until = until });
        return this;
    }

    public BcEntityBuilder Fields(params string[] names)
    {
        foreach (var name in names)
        {
            _entry.Fields.Add(new BcFieldEntry { Name = name });
        }
        return this;
    }

    public BcEntityBuilder Fields(BcApiVersion since, params string[] names)
    {
        foreach (var name in names)
        {
            _entry.Fields.Add(new BcFieldEntry { Name = name, Since = since });
        }
        return this;
    }

    internal BcEntityEntry Build() => _entry;
}
