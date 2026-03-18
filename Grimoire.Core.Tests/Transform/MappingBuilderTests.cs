using Grimoire.Core.Load;
using Grimoire.Core.Transform;

namespace Grimoire.Core.Tests.Transform;

public class MappingBuilderTests
{
    [Fact]
    public void Map_creates_property_mapping()
    {
        var mapping = new CustomerMapping();
        var built = mapping.Build();

        Assert.Equal(4, built.Mappings.Count);
        Assert.Equal("customer_id", built.Mappings[0].SourceColumn);
        Assert.Equal("Id", built.Mappings[0].TargetProperty.Name);
    }

    [Fact]
    public void FromTables_registers_source_tables()
    {
        var mapping = new CustomerMapping();
        var built = mapping.Build();

        Assert.Single(built.SourceTables);
        Assert.Equal("Customers", built.SourceTables[0]);
    }

    [Fact]
    public void AsForeignKey_sets_foreign_key_entity_type()
    {
        var mapping = new CustomerWithForeignKeyMapping();
        var built = mapping.Build();

        var fkMapping = built.Mappings.First(m => m.SourceColumn == "legacy_dept_id");
        Assert.Equal(typeof(Department), fkMapping.ForeignKeyEntityType);
    }

    [Fact]
    public void Default_sets_default_value()
    {
        var mapping = new CustomerWithDefaultsMapping();
        var built = mapping.Build();

        var nameMapping = built.Mappings.First(m => m.SourceColumn == "customer_name");
        Assert.True(nameMapping.HasDefault);
        Assert.Equal("Unknown", nameMapping.DefaultValue);
    }

    [Fact]
    public void Converter_sets_converter_function()
    {
        var mapping = new CustomerWithConverterMapping();
        var built = mapping.Build();

        var nameMapping = built.Mappings.First(m => m.SourceColumn == "customer_name");
        Assert.NotNull(nameMapping.Converter);
        Assert.Equal("HELLO", nameMapping.Converter("hello"));
    }

    [Fact]
    public void Map_handles_value_type_properties()
    {
        // Value types like int get boxed in Expression<Func<T, object?>>
        // ExtractMember should unwrap the Convert node
        var mapping = new CustomerMapping();
        var built = mapping.Build();

        var idMapping = built.Mappings.First(m => m.SourceColumn == "customer_id");
        Assert.Equal("Id", idMapping.TargetProperty.Name);
    }
}
