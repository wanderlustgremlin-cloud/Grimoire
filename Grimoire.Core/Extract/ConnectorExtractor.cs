using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;

namespace Grimoire.Core.Extract;

internal sealed class ConnectorExtractor
{
    private readonly IConnector _connector;

    public ConnectorExtractor(IConnector connector)
    {
        _connector = connector;
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

    private string Q(string identifier) => _connector.Provider switch
    {
        DatabaseProvider.SqlServer => $"[{identifier}]",
        DatabaseProvider.MySql => $"`{identifier}`",
        _ => $"\"{identifier}\""  // Postgres, Oracle use double quotes
    };

    private string BuildQuery(ExtractRequest request, Dictionary<string, TableSchema> schemas)
    {
        if (request.SourceTables.Count == 0)
            throw new InvalidOperationException($"Entity '{request.EntityName}' has no source tables configured. Call FromTables() in the mapping.");

        var primaryTable = request.SourceTables[0];
        if (!schemas.TryGetValue(primaryTable, out var primarySchema))
            throw new InvalidOperationException($"Table '{primaryTable}' is not defined in the connector schema.");

        var sb = new StringBuilder();

        // SELECT
        if (request.SourceColumns.Count > 0)
        {
            var qualifiedColumns = request.SourceColumns.Select(col =>
            {
                // Try to find which table has this column
                foreach (var tableName in request.SourceTables)
                {
                    if (schemas.TryGetValue(tableName, out var schema) && schema.Columns.Count > 0)
                    {
                        if (schema.Columns.Contains(col, StringComparer.OrdinalIgnoreCase))
                            return $"{Q(tableName)}.{Q(col)}";
                    }
                }
                // Default to primary table
                return $"{Q(primaryTable)}.{Q(col)}";
            });
            sb.Append("SELECT ").AppendJoin(", ", qualifiedColumns);
        }
        else
        {
            sb.Append($"SELECT {Q(primaryTable)}.*");
        }

        // FROM
        sb.Append($" FROM {Q(primaryTable)}");

        // JOINs
        for (int i = 1; i < request.SourceTables.Count; i++)
        {
            var joinTable = request.SourceTables[i];
            var join = FindJoin(primarySchema, joinTable, schemas);
            if (join is not null)
            {
                sb.Append($" INNER JOIN {Q(join.ToTable)} ON {Q(join.FromTable)}.{Q(join.FromColumn)} = {Q(join.ToTable)}.{Q(join.ToColumn)}");
            }
            else
            {
                throw new InvalidOperationException(
                    $"No join defined between '{primaryTable}' and '{joinTable}'. Define joins in the connector's ConfigureSchema.");
            }
        }

        return sb.ToString();
    }

    private static JoinDefinition? FindJoin(TableSchema primarySchema, string targetTable, Dictionary<string, TableSchema> schemas)
    {
        // Check primary table's joins
        var join = primarySchema.Joins.FirstOrDefault(j =>
            j.ToTable.Equals(targetTable, StringComparison.OrdinalIgnoreCase));
        if (join is not null) return join;

        // Check target table's joins back to primary
        if (schemas.TryGetValue(targetTable, out var targetSchema))
        {
            join = targetSchema.Joins.FirstOrDefault(j =>
                j.ToTable.Equals(primarySchema.TableName, StringComparison.OrdinalIgnoreCase));
            if (join is not null)
            {
                // Reverse the join direction
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
