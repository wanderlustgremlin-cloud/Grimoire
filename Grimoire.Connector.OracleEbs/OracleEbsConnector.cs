using System.Data.Common;
using Grimoire.Connector.OracleEbs.Modules;
using Grimoire.Connector.OracleEbs.Registry;
using Grimoire.Core.Extract;
using Oracle.ManagedDataAccess.Client;

namespace Grimoire.Connector.OracleEbs;

public sealed class OracleEbsConnector : IConnector
{
    private readonly string _connectionString;
    private readonly EbsVersion _version;
    private readonly EbsSchemaMode _schemaMode;
    private readonly EbsModuleSelector _moduleSelector;
    private readonly EbsRegistry _registry;

    public OracleEbsConnector(
        string connectionString,
        EbsVersion version,
        Action<EbsModuleSelector> configureModules,
        EbsSchemaMode schemaMode = EbsSchemaMode.Apps)
    {
        _connectionString = connectionString;
        _version = version;
        _schemaMode = schemaMode;

        _moduleSelector = new EbsModuleSelector();
        configureModules(_moduleSelector);

        if (_moduleSelector.SelectedModules.Count == 0)
            throw new InvalidOperationException("At least one EBS module must be selected.");

        _registry = new EbsRegistry();
        RegisterModules();
    }

    public DatabaseProvider Provider => DatabaseProvider.Oracle;

    public DbConnection CreateConnection() => new OracleConnection(_connectionString);

    public void ConfigureSchema(ISchemaBuilder schema)
    {
        _registry.WriteToSchema(schema, _version, _schemaMode,
            _moduleSelector.SelectedModules, _moduleSelector.SelectedTables);
    }

    private void RegisterModules()
    {
        var modules = new IEbsModule[]
        {
            new HrModule(),
            new ApModule(),
            new ArModule(),
            new GlModule(),
            new PoModule(),
            new InvModule()
        };

        foreach (var module in modules)
        {
            if (_moduleSelector.SelectedModules.Contains(module.ModuleName))
            {
                module.Register(_registry);
            }
        }
    }
}
