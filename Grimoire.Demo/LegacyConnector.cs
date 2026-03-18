using System.Data.Common;
using Grimoire.Core.Extract;
using Microsoft.Data.SqlClient;

namespace Grimoire.Demo;

public class LegacyConnector(string connectionString) : IConnector
{
    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public DbConnection CreateConnection() => new SqlConnection(connectionString);

    public void ConfigureSchema(ISchemaBuilder schema)
    {
        schema
            .Table("LegacyEmployees")
                .Columns("EmpId", "FullName", "Email", "DeptName", "HireDate", "IsActive")
                .JoinTo("LegacyResponsibilities", "EmpId", "EmpId")
                .Done()
            .Table("LegacyResponsibilities")
                .Columns("ResponsibilityId", "EmpId", "Responsibility", "AssignedDate")
                .Done();
    }
}
