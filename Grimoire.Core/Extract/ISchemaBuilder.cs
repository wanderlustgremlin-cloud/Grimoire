namespace Grimoire.Core.Extract;

public interface ISchemaBuilder
{
    ITableSchemaBuilder Table(string tableName);
    Dictionary<string, TableSchema> Build();
}

public interface ITableSchemaBuilder
{
    ITableSchemaBuilder Columns(params string[] columns);
    ITableSchemaBuilder JoinTo(string toTable, string fromColumn, string toColumn);
    ISchemaBuilder Done();
}
