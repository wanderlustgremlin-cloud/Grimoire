using Grimoire.Core.Extract;

namespace Grimoire.Core.Tests.Extract;

public class SchemaBuilderTests
{
    [Fact]
    public void Builds_single_table_with_columns()
    {
        var builder = new SchemaBuilder();
        builder.Table("Customers")
            .Columns("Id", "Name", "Email")
            .Done();

        var schemas = builder.Build();

        Assert.Single(schemas);
        Assert.True(schemas.ContainsKey("Customers"));
        Assert.Equal(["Id", "Name", "Email"], schemas["Customers"].Columns);
    }

    [Fact]
    public void Builds_multiple_tables()
    {
        var builder = new SchemaBuilder();
        builder.Table("Customers").Columns("Id", "Name").Done()
               .Table("Orders").Columns("Id", "CustomerId").Done();

        var schemas = builder.Build();

        Assert.Equal(2, schemas.Count);
        Assert.True(schemas.ContainsKey("Customers"));
        Assert.True(schemas.ContainsKey("Orders"));
    }

    [Fact]
    public void Builds_table_with_join()
    {
        var builder = new SchemaBuilder();
        builder.Table("Customers")
            .Columns("Id", "Name")
            .JoinTo("Orders", "Id", "CustomerId")
            .Done();

        var schemas = builder.Build();
        var join = Assert.Single(schemas["Customers"].Joins);

        Assert.Equal("Customers", join.FromTable);
        Assert.Equal("Id", join.FromColumn);
        Assert.Equal("Orders", join.ToTable);
        Assert.Equal("CustomerId", join.ToColumn);
    }

    [Fact]
    public void Table_lookup_is_case_insensitive()
    {
        var builder = new SchemaBuilder();
        builder.Table("Customers").Columns("Id").Done();

        var schemas = builder.Build();

        Assert.True(schemas.ContainsKey("customers"));
        Assert.True(schemas.ContainsKey("CUSTOMERS"));
    }

    [Fact]
    public void Build_flushes_last_table_without_Done()
    {
        var builder = new SchemaBuilder();
        builder.Table("Customers").Columns("Id", "Name");

        var schemas = builder.Build();

        Assert.Single(schemas);
        Assert.True(schemas.ContainsKey("Customers"));
    }
}
