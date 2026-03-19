using Grimoire.Connector.BusinessCentral.Registry;

namespace Grimoire.Connector.BusinessCentral.Modules;

internal interface IBcModule
{
    string ModuleName { get; }
    void Register(BcRegistry registry);
}
