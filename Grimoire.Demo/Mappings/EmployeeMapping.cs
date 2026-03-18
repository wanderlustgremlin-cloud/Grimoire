using Grimoire.Core.Transform;
using Grimoire.Demo.Entities;

namespace Grimoire.Demo.Mappings;

public class EmployeeMapping : GrimoireMapping<Employee>
{
    public override void Configure(IMappingBuilder<Employee> builder)
    {
        builder.FromTables("LegacyEmployees");

        builder.Map(e => e.FirstName, "FullName")
            .Convert(v =>
            {
                var name = v?.ToString() ?? "";
                var parts = name.Split(' ', 2);
                return parts[0];
            });

        builder.Map(e => e.LastName, "FullName")
            .Convert(v =>
            {
                var name = v?.ToString() ?? "";
                var parts = name.Split(' ', 2);
                return parts.Length > 1 ? parts[1] : "";
            });

        builder.Map(e => e.Email, "Email");

        builder.Map(e => e.DepartmentId, "DeptName")
            .AsForeignKey<Department>();

        builder.Map(e => e.HireDate, "HireDate");

        builder.Map(e => e.IsActive, "IsActive")
            .Default(true);
    }
}
