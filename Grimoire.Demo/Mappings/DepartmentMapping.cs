using Grimoire.Core.Transform;
using Grimoire.Demo.Entities;

namespace Grimoire.Demo.Mappings;

public class DepartmentMapping : GrimoireMapping<Department>
{
    public override void Configure(IMappingBuilder<Department> builder)
    {
        builder.Map(d => d.Name, "DeptName");
    }
}
