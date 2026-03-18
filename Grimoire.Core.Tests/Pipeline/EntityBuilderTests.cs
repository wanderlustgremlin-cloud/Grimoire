using Grimoire.Core.Load;
using Grimoire.Core.Pipeline;
using Grimoire.Core.Transform;

namespace Grimoire.Core.Tests.Pipeline;

public class EntityBuilderTests
{
    [Fact]
    public void Entity_sets_type_and_name()
    {
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>();

        Assert.Equal(typeof(Customer), builder.Registration.EntityType);
        Assert.Equal("Customer", builder.Registration.EntityName);
    }

    [Fact]
    public void TransformUsing_generic_sets_mapping()
    {
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>()
            .TransformUsing<CustomerMapping>();

        Assert.NotNull(builder.Registration.Mapping);
        Assert.IsType<CustomerMapping>(builder.Registration.Mapping);
    }

    [Fact]
    public void TransformUsing_instance_sets_mapping()
    {
        var mapping = new CustomerMapping();
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>()
            .TransformUsing(mapping);

        Assert.Same(mapping, builder.Registration.Mapping);
    }

    [Fact]
    public void LoadInto_sets_load_config()
    {
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>()
            .LoadInto("Customers", "Server=.;Database=Test", batchSize: 500);

        Assert.NotNull(builder.Registration.LoadConfig);
        Assert.Equal("Customers", builder.Registration.LoadConfig.TargetTable);
        Assert.Equal("Server=.;Database=Test", builder.Registration.LoadConfig.ConnectionString);
        Assert.Equal(500, builder.Registration.LoadConfig.BatchSize);
    }

    [Fact]
    public void LoadInto_default_batch_size_is_1000()
    {
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>()
            .LoadInto("Customers", "Server=.;Database=Test");

        Assert.Equal(1000, builder.Registration.LoadConfig!.BatchSize);
    }

    [Fact]
    public void LoadInto_config_overload_sets_config()
    {
        var config = new LoadConfig { TargetTable = "Cust", ConnectionString = "conn", BatchSize = 250 };
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>().LoadInto(config);

        Assert.Same(config, builder.Registration.LoadConfig);
    }

    [Fact]
    public void DependsOn_adds_dependency()
    {
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>()
            .DependsOn<Department>();

        Assert.Single(builder.Registration.Dependencies);
        Assert.Equal(typeof(Department), builder.Registration.Dependencies[0]);
    }

    [Fact]
    public void DependsOn_multiple_adds_all()
    {
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Order>()
            .DependsOn<Customer>()
            .DependsOn<Department>();

        Assert.Equal(2, builder.Registration.Dependencies.Count);
        Assert.Contains(typeof(Customer), builder.Registration.Dependencies);
        Assert.Contains(typeof(Department), builder.Registration.Dependencies);
    }

    [Fact]
    public void TrackKey_sets_key_properties()
    {
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>()
            .TrackKey("Id", "legacy_id");

        Assert.Equal("Id", builder.Registration.TrackKeyProperty);
        Assert.Equal("legacy_id", builder.Registration.TrackKeyLegacyColumn);
    }

    [Fact]
    public void MatchOn_sets_match_config()
    {
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>()
            .MatchOn(m => m.Columns("Email").WhenMatched(UpdateStrategy.OverwriteChanged));

        Assert.NotNull(builder.Registration.MatchConfig);
        Assert.Equal(["Email"], builder.Registration.MatchConfig.MatchColumns);
        Assert.Equal(UpdateStrategy.OverwriteChanged, builder.Registration.MatchConfig.WhenMatchedStrategy);
    }

    [Fact]
    public void ExtractUsing_sets_custom_extractor()
    {
        var extractor = new InMemoryExtractor([]);
        var pipeline = new GrimoirePipeline();
        var builder = pipeline.Entity<Customer>()
            .ExtractUsing(extractor);

        Assert.Same(extractor, builder.Registration.CustomExtractor);
    }

    [Fact]
    public void Done_returns_pipeline_for_chaining()
    {
        var pipeline = new GrimoirePipeline();

        var result = pipeline.Entity<Customer>()
            .TransformUsing<CustomerMapping>()
            .LoadInto("Customers", "conn")
            .Done();

        Assert.Same(pipeline, result);
    }

    [Fact]
    public void Fluent_chain_configures_everything()
    {
        var pipeline = new GrimoirePipeline();
        var extractor = new InMemoryExtractor([]);

        pipeline
            .Entity<Department>()
                .LoadInto("Departments", "conn", 2000)
                .ExtractUsing(extractor)
                .Done()
            .Entity<Customer>()
                .TransformUsing<CustomerMapping>()
                .ExtractUsing(extractor)
                .LoadInto("Customers", "conn")
                .DependsOn<Department>()
                .TrackKey("Id", "legacy_id")
                .MatchOn(m => m.Columns("Email").WhenMatched(UpdateStrategy.Skip))
                .Done();

        // If we got here without exception, the fluent chain works
    }
}
