namespace Grimoire.Connector.BusinessCentral.Registry;

internal sealed class BcFieldEntry
{
    public required string Name { get; init; }
    public BcApiVersion Since { get; init; } = BcApiVersion.V1_0;
    public BcApiVersion? Until { get; init; }

    public bool ExistsIn(BcApiVersion version)
        => version >= Since && (Until is null || version < Until);
}

internal sealed class BcEntityEntry
{
    public required string EntityName { get; init; }
    public required string ApiPath { get; init; }
    public required string Module { get; init; }
    public BcApiVersion Since { get; init; } = BcApiVersion.V1_0;
    public BcApiVersion? Until { get; init; }
    public List<BcFieldEntry> Fields { get; } = [];

    public bool ExistsIn(BcApiVersion version)
        => version >= Since && (Until is null || version < Until);

    public IReadOnlyList<string> GetFieldsForVersion(BcApiVersion version)
        => Fields.Where(f => f.ExistsIn(version)).Select(f => f.Name).ToList();
}
