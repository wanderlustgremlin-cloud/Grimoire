using Grimoire.Connector.BusinessCentral.Registry;

namespace Grimoire.Connector.BusinessCentral.Modules;

internal sealed class HrModule : IBcModule
{
    public string ModuleName => "HR";

    public void Register(BcRegistry registry)
    {
        registry.Entity("employees", "employees", "HR")
            .Fields("id", "number",
                "givenName", "middleName", "surname", "displayName",
                "jobTitle",
                "addressLine1", "addressLine2",
                "city", "state", "country", "postalCode",
                "phoneNumber", "mobilePhone", "email",
                "personalEmail",
                "employmentDate", "terminationDate",
                "status",
                "birthDate",
                "statisticsGroupCode",
                "lastModifiedDateTime");
    }
}
