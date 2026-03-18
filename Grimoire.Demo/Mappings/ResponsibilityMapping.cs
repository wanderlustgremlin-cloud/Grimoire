using Grimoire.Core.Transform;
using Grimoire.Demo.Entities;

namespace Grimoire.Demo.Mappings;

public class ResponsibilityMapping : GrimoireMapping<Responsibility>
{
    public override void Configure(IMappingBuilder<Responsibility> builder)
    {
        builder.FromTables("LegacyResponsibilities");

        builder.Map(r => r.EmployeeId, "EmpId")
            .AsForeignKey<Employee>();

        builder.Map(r => r.Title, "Responsibility");

        builder.Map(r => r.AssignedDate, "AssignedDate");
    }
}
