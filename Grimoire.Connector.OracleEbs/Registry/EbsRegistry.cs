using Grimoire.Core.Extract;

namespace Grimoire.Connector.OracleEbs.Registry;

internal sealed class EbsRegistry
{
    private readonly List<EbsTableEntry> _tables = [];

    public EbsTableBuilder Table(string appsView, string baseTable, string baseSchema, string module)
    {
        var builder = new EbsTableBuilder(appsView, baseTable, baseSchema, module);
        _tables.Add(builder.Build());
        return builder;
    }

    public void WriteToSchema(
        ISchemaBuilder schemaBuilder,
        EbsVersion version,
        EbsSchemaMode mode,
        HashSet<string> selectedModules,
        HashSet<string>? selectedTables)
    {
        foreach (var table in _tables)
        {
            if (!table.ExistsIn(version)) continue;
            if (!selectedModules.Contains(table.Module)) continue;
            if (selectedTables is not null && !selectedTables.Contains(table.AppsView)) continue;

            var tableName = mode == EbsSchemaMode.Apps ? table.AppsView : table.BaseTable;
            var tableSchema = mode == EbsSchemaMode.Apps ? "APPS" : table.BaseSchema;

            var columns = table.Columns
                .Where(c => c.ExistsIn(version))
                .Select(c => c.Name)
                .ToArray();

            var tableBuilder = schemaBuilder.Table(tableName, schema: tableSchema);

            if (columns.Length > 0)
                tableBuilder.Columns(columns);

            foreach (var join in table.Joins.Where(j => j.ExistsIn(version)))
            {
                // Resolve the join target table name based on mode
                var joinTarget = ResolveTableName(join.ToTable, mode, version);
                if (joinTarget is not null)
                    tableBuilder.JoinTo(joinTarget, join.FromColumn, join.ToColumn);
            }

            tableBuilder.Done();
        }
    }

    private string? ResolveTableName(string appsViewName, EbsSchemaMode mode, EbsVersion version)
    {
        var target = _tables.FirstOrDefault(t =>
            t.AppsView.Equals(appsViewName, StringComparison.OrdinalIgnoreCase) && t.ExistsIn(version));
        if (target is null) return appsViewName;
        return mode == EbsSchemaMode.Apps ? target.AppsView : target.BaseTable;
    }

    public IReadOnlyList<string> GetTableNames(EbsVersion version, EbsSchemaMode mode, string module)
    {
        return _tables
            .Where(t => t.ExistsIn(version) && t.Module.Equals(module, StringComparison.OrdinalIgnoreCase))
            .Select(t => mode == EbsSchemaMode.Apps ? t.AppsView : t.BaseTable)
            .ToList();
    }
}
