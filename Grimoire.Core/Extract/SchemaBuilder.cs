namespace Grimoire.Core.Extract;

public sealed class SchemaBuilder : ISchemaBuilder
{
    private readonly Dictionary<string, TableSchema> _tables = new(StringComparer.OrdinalIgnoreCase);
    private TableSchemaBuilderImpl? _current;

    public ITableSchemaBuilder Table(string tableName)
    {
        FlushCurrent();
        _current = new TableSchemaBuilderImpl(this, tableName);
        return _current;
    }

    public Dictionary<string, TableSchema> Build()
    {
        FlushCurrent();
        return new Dictionary<string, TableSchema>(_tables, StringComparer.OrdinalIgnoreCase);
    }

    private void FlushCurrent()
    {
        if (_current is null) return;
        var schema = _current.BuildSchema();
        _tables[schema.TableName] = schema;
        _current = null;
    }

    private sealed class TableSchemaBuilderImpl(SchemaBuilder parent, string tableName) : ITableSchemaBuilder
    {
        private readonly List<string> _columns = [];
        private readonly List<JoinDefinition> _joins = [];

        public ITableSchemaBuilder Columns(params string[] columns)
        {
            _columns.AddRange(columns);
            return this;
        }

        public ITableSchemaBuilder JoinTo(string toTable, string fromColumn, string toColumn)
        {
            _joins.Add(new JoinDefinition
            {
                FromTable = tableName,
                FromColumn = fromColumn,
                ToTable = toTable,
                ToColumn = toColumn
            });
            return this;
        }

        public ISchemaBuilder Done()
        {
            return parent;
        }

        public TableSchema BuildSchema() => new()
        {
            TableName = tableName,
            Columns = [.. _columns],
            Joins = [.. _joins]
        };
    }
}
