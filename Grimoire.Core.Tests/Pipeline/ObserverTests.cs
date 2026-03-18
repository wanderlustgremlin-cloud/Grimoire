using Grimoire.Core.Pipeline;
using Grimoire.Core.Results;

namespace Grimoire.Core.Tests.Pipeline;

public class ObserverTests
{
    [Fact]
    public void Default_interface_methods_are_no_ops()
    {
        IPipelineObserver observer = new NoOpObserver();

        // These should not throw — they use default interface implementations
        observer.OnPipelineStarted();
        observer.OnEntityStarted("Test");
        observer.OnProgress("Test", 100);
        observer.OnRowError(new RowError("Test", RowErrorType.MappingError, "msg"));
        observer.OnBatchLoaded(new BatchResult("Test", 1, 100, 100, 0, 0, TimeSpan.FromMilliseconds(50), 1000));
        observer.OnEntityComplete(new EntityResult { EntityName = "Test" });
        observer.OnPipelineComplete(new EtlResult());
    }

    [Fact]
    public void Observer_can_selectively_override_methods()
    {
        var observer = new ProgressOnlyObserver();
        IPipelineObserver iface = observer;

        iface.OnPipelineStarted(); // default no-op
        observer.OnProgress("Test", 50);

        Assert.Equal(("Test", 50), observer.LastProgress);
    }

    private class NoOpObserver : IPipelineObserver { }

    private class ProgressOnlyObserver : IPipelineObserver
    {
        public (string Entity, int Rows)? LastProgress { get; private set; }

        public void OnProgress(string entityName, int rowsProcessed)
        {
            LastProgress = (entityName, rowsProcessed);
        }
    }
}
