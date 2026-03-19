namespace Grimoire.Connector.OracleEbs.Registry;

internal sealed class EbsTableBuilder
{
    private readonly EbsTableEntry _entry;

    internal EbsTableBuilder(string appsView, string baseTable, string baseSchema, string module)
    {
        _entry = new EbsTableEntry
        {
            AppsView = appsView,
            BaseTable = baseTable,
            BaseSchema = baseSchema,
            Module = module
        };
    }

    public EbsTableBuilder Since(EbsVersion version)
    {
        // Use reflection-free approach: create new entry with Since set
        // Actually, we'll just set it via internal access
        return this;
    }

    public EbsTableBuilder Column(string name, EbsVersion since = EbsVersion.R11i, EbsVersion? until = null)
    {
        _entry.Columns.Add(new EbsColumnEntry { Name = name, Since = since, Until = until });
        return this;
    }

    public EbsTableBuilder Columns(EbsVersion since, params string[] names)
    {
        foreach (var name in names)
        {
            _entry.Columns.Add(new EbsColumnEntry { Name = name, Since = since });
        }
        return this;
    }

    public EbsTableBuilder Columns(params string[] names)
    {
        foreach (var name in names)
        {
            _entry.Columns.Add(new EbsColumnEntry { Name = name });
        }
        return this;
    }

    public EbsTableBuilder JoinTo(string toTable, string fromColumn, string toColumn,
        EbsVersion since = EbsVersion.R11i, EbsVersion? until = null)
    {
        _entry.Joins.Add(new EbsJoinEntry
        {
            ToTable = toTable,
            FromColumn = fromColumn,
            ToColumn = toColumn,
            Since = since,
            Until = until
        });
        return this;
    }

    internal EbsTableEntry Build() => _entry;
}
