using System.Runtime.CompilerServices;
using Grimoire.Core.Extract;
using Microsoft.Data.SqlClient;

namespace Grimoire.Demo;

public class LegacyDepartmentExtractor(string connectionString) : ICustomExtractor
{
    public async IAsyncEnumerable<SourceRow> ExtractAsync(
        ExtractRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT DeptName FROM LegacyEmployees ORDER BY DeptName";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new SourceRow();
            row["DeptName"] = reader["DeptName"];
            yield return row;
        }
    }
}

public class LegacyEmployeeExtractor(string connectionString) : ICustomExtractor
{
    public async IAsyncEnumerable<SourceRow> ExtractAsync(
        ExtractRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EmpId, FullName, Email, DeptName, HireDate, IsActive FROM LegacyEmployees";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new SourceRow();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            yield return row;
        }
    }
}
