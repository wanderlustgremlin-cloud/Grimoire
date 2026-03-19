using Grimoire.Connector.OracleEbs.Registry;

namespace Grimoire.Connector.OracleEbs.Modules;

internal interface IEbsModule
{
    string ModuleName { get; }
    void Register(EbsRegistry registry);
}
