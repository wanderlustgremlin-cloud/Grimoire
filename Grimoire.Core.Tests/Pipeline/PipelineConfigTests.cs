using Grimoire.Core.Load;
using Grimoire.Core.Pipeline;
using Grimoire.Core.Results;

namespace Grimoire.Core.Tests.Pipeline;

public class PipelineConfigTests
{
    [Fact]
    public async Task ExecuteAsync_throws_when_entity_has_no_load_config()
    {
        var pipeline = new GrimoirePipeline();
        pipeline.Entity<Customer>()
            .TransformUsing<CustomerMapping>()
            .ExtractUsing(new InMemoryExtractor([]))
            .Done(); // no LoadInto

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_entity_has_no_mapping()
    {
        var provider = new StubTargetProvider();
        var pipeline = new GrimoirePipeline();
        pipeline.LoadWith(provider)
            .Entity<Customer>()
            .ExtractUsing(new InMemoryExtractor([]))
            .LoadInto("Customers")
            .Done(); // no TransformUsing

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_entity_has_no_data_source()
    {
        var provider = new StubTargetProvider();
        var pipeline = new GrimoirePipeline();
        pipeline.LoadWith(provider)
            .Entity<Customer>()
            .TransformUsing<CustomerMapping>()
            .LoadInto("Customers")
            .Done(); // no ExtractUsing or ExtractFrom

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_entity_has_no_target_provider()
    {
        var pipeline = new GrimoirePipeline();
        pipeline.Entity<Customer>()
            .TransformUsing<CustomerMapping>()
            .ExtractUsing(new InMemoryExtractor([]))
            .LoadInto("Customers")
            .Done(); // no LoadWith or provider in LoadInto

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync());
    }

    [Fact]
    public void AddObserver_returns_pipeline_for_chaining()
    {
        var pipeline = new GrimoirePipeline();
        var observer = new TestObserver();

        var result = pipeline.AddObserver(observer);

        Assert.Same(pipeline, result);
    }

    [Fact]
    public void OnRowError_returns_pipeline_for_chaining()
    {
        var pipeline = new GrimoirePipeline();

        var result = pipeline.OnRowError(_ => { });

        Assert.Same(pipeline, result);
    }

    [Fact]
    public void OnProgress_returns_pipeline_for_chaining()
    {
        var pipeline = new GrimoirePipeline();

        var result = pipeline.OnProgress((_, _) => { });

        Assert.Same(pipeline, result);
    }

    [Fact]
    public void OnEntityComplete_returns_pipeline_for_chaining()
    {
        var pipeline = new GrimoirePipeline();

        var result = pipeline.OnEntityComplete(_ => { });

        Assert.Same(pipeline, result);
    }

    private class TestObserver : IPipelineObserver { }

    private class StubTargetProvider : ITargetProvider
    {
        public Task<ITargetSession> BeginSessionAsync(string targetTable, CancellationToken ct)
            => throw new NotImplementedException();
    }
}
