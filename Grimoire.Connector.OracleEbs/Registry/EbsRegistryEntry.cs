namespace Grimoire.Connector.OracleEbs.Registry;

internal sealed class EbsColumnEntry
{
    public required string Name { get; init; }
    public EbsVersion Since { get; init; } = EbsVersion.R11i;
    public EbsVersion? Until { get; init; }

    public bool ExistsIn(EbsVersion version)
        => version >= Since && (Until is null || version < Until);
}

internal sealed class EbsJoinEntry
{
    public required string ToTable { get; init; }
    public required string FromColumn { get; init; }
    public required string ToColumn { get; init; }
    public EbsVersion Since { get; init; } = EbsVersion.R11i;
    public EbsVersion? Until { get; init; }

    public bool ExistsIn(EbsVersion version)
        => version >= Since && (Until is null || version < Until);
}

internal sealed class EbsTableEntry
{
    public required string AppsView { get; init; }
    public required string BaseTable { get; init; }
    public required string BaseSchema { get; init; }
    public required string Module { get; init; }
    public EbsVersion Since { get; init; } = EbsVersion.R11i;
    public EbsVersion? Until { get; init; }
    public List<EbsColumnEntry> Columns { get; } = [];
    public List<EbsJoinEntry> Joins { get; } = [];

    public bool ExistsIn(EbsVersion version)
        => version >= Since && (Until is null || version < Until);
}
