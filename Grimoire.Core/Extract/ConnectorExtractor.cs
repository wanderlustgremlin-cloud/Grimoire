using System.Runtime.CompilerServices;
using System.Text;
using Grimoire.Core.Extract.Dialects;

namespace Grimoire.Core.Extract;

internal sealed class ConnectorExtractor
{
    private readonly IConnector _connector;
    private readonly ISqlDialect _dialect;

    public ConnectorExtractor(IConnector connector)
    {
        _connector = connector;
        _dialect = connector.Dialect ?? DialectFactory.Create(connector.Provider);
    }

    public async IAsyncEnumerable<SourceRow> ExtractAsync(
        ExtractRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var schemaBuilder = new SchemaBuilder();
        _connector.ConfigureSchema(schemaBuilder);
        var schemas = schemaBuilder.Build();

        var sql = BuildQuery(request, schemas);

        await using var connection = _connector.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new SourceRow();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[name] = value;
            }
            yield return row;
        }
    }

    private string QualifyTable(string tableName, Dictionary<string, TableSchema> schemas)
    {
        var schema = schemas.TryGetValue(tableName, out var ts) ? ts.Schema : null;
        return _dialect.QualifyTable(tableName, schema);
    }

    private string BuildQuery(ExtractRequest request, Dictionary<string, TableSchema> schemas)
    {
        if (request.SourceTables.Count == 0)
            throw new InvalidOperationException($"Entity '{request.EntityName}' has no source tables configured. Call FromTables() in the mapping.");

        var primaryTable = request.SourceTables[0];
        if (!schemas.TryGetValue(primaryTable, out var primarySchema))
            throw new InvalidOperationException($"Table '{primaryTable}' is not defined in the connector schema.");

        var sb = new StringBuilder();
        var q = _dialect;

        // SELECT
        if (request.SourceColumns.Count > 0)
        {
            var qualifiedColumns = request.SourceColumns.Select(col =>
            {
                foreach (var tableName in request.SourceTables)
                {
                    if (schemas.TryGetValue(tableName, out var schema) && schema.Columns.Count > 0)
                    {
                        if (schema.Columns.Contains(col, StringComparer.OrdinalIgnoreCase))
                            return $"{QualifyTable(tableName, schemas)}.{q.QuoteIdentifier(col)}";
                    }
                }
                return $"{QualifyTable(primaryTable, schemas)}.{q.QuoteIdentifier(col)}";
            });
            sb.Append("SELECT ").AppendJoin(", ", qualifiedColumns);
        }
        else
        {
            sb.Append($"SELECT {QualifyTable(primaryTable, schemas)}.*");
        }

        // FROM
        sb.Append($" FROM {QualifyTable(primaryTable, schemas)}");

        // JOINs
        for (int i = 1; i < request.SourceTables.Count; i++)
        {
            var joinTable = request.SourceTables[i];
            var join = FindJoin(primarySchema, joinTable, schemas);
            if (join is not null)
            {
                sb.Append($" INNER JOIN {QualifyTable(join.ToTable, schemas)} ON {QualifyTable(join.FromTable, schemas)}.{q.QuoteIdentifier(join.FromColumn)} = {QualifyTable(join.ToTable, schemas)}.{q.QuoteIdentifier(join.ToColumn)}");
            }
            else
            {
                throw new InvalidOperationException(
                    $"No join defined between '{primaryTable}' and '{joinTable}'. Define joins in the connector's ConfigureSchema.");
            }
        }

        // Pagination
        var pagination = q.BuildPagination(request.Limit, request.Offset);
        if (pagination.Length > 0)
            sb.Append(pagination);

        return sb.ToString();
    }

    private static JoinDefinition? FindJoin(TableSchema primarySchema, string targetTable, Dictionary<string, TableSchema> schemas)
    {
        var join = primarySchema.Joins.FirstOrDefault(j =>
            j.ToTable.Equals(targetTable, StringComparison.OrdinalIgnoreCase));
        if (join is not null) return join;

        if (schemas.TryGetValue(targetTable, out var targetSchema))
        {
            join = targetSchema.Joins.FirstOrDefault(j =>
                j.ToTable.Equals(primarySchema.TableName, StringComparison.OrdinalIgnoreCase));
            if (join is not null)
            {
                return new JoinDefinition
                {
                    FromTable = join.ToTable,
                    FromColumn = join.ToColumn,
                    ToTable = join.FromTable,
                    ToColumn = join.FromColumn
                };
            }
        }

        return null;
    }
}
