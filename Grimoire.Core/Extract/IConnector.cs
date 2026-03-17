using System.Data.Common;

namespace Grimoire.Core.Extract;

public interface IConnector
{
    DatabaseProvider Provider { get; }
    DbConnection CreateConnection();
    void ConfigureSchema(ISchemaBuilder schema);
}
