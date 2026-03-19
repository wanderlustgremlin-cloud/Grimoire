using System.Data.Common;

namespace Grimoire.Core.Extract;

public interface IConnector
{
    DatabaseProvider Provider { get; }
    ISqlDialect? Dialect => null;
    DbConnection CreateConnection();
    void ConfigureSchema(ISchemaBuilder schema);
}
