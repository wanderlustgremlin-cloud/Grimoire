using Grimoire.Core.Extract;
using Grimoire.Core.Transform;

namespace Grimoire.Core.Tests;

// Shared test entity types
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; }
}

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
}

// Test mapping classes
public class CustomerMapping : GrimoireMapping<Customer>
{
    public override void Configure(IMappingBuilder<Customer> builder)
    {
        builder.FromTables("Customers");
        builder.Map(c => c.Id, "customer_id");
        builder.Map(c => c.Name, "customer_name");
        builder.Map(c => c.Email, "email");
        builder.Map(c => c.IsActive, "is_active");
    }
}

public class CustomerWithForeignKeyMapping : GrimoireMapping<Customer>
{
    public override void Configure(IMappingBuilder<Customer> builder)
    {
        builder.Map(c => c.Id, "customer_id");
        builder.Map(c => c.Name, "customer_name");
        builder.Map(c => c.DepartmentId, "legacy_dept_id").AsForeignKey<Department>();
    }
}

public class CustomerWithDefaultsMapping : GrimoireMapping<Customer>
{
    public override void Configure(IMappingBuilder<Customer> builder)
    {
        builder.Map(c => c.Id, "customer_id");
        builder.Map(c => c.Name, "customer_name").Default("Unknown");
        builder.Map(c => c.IsActive, "is_active").Default(true);
    }
}

public class CustomerWithConverterMapping : GrimoireMapping<Customer>
{
    public override void Configure(IMappingBuilder<Customer> builder)
    {
        builder.Map(c => c.Id, "customer_id");
        builder.Map(c => c.Name, "customer_name")
            .Convert(v => v?.ToString()?.ToUpperInvariant());
    }
}

// Test custom extractor
public class InMemoryExtractor : ICustomExtractor
{
    private readonly List<SourceRow> _rows;

    public InMemoryExtractor(List<SourceRow> rows)
    {
        _rows = rows;
    }

    public async IAsyncEnumerable<SourceRow> ExtractAsync(
        ExtractRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var row in _rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
        }
        await Task.CompletedTask;
    }
}
