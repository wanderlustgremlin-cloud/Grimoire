using Grimoire.Core.Pipeline;

namespace Grimoire.Observability.OpenTelemetry;

public static class PipelineExtensions
{
    public static GrimoirePipeline AddTracing(this GrimoirePipeline pipeline)
    {
        return pipeline.AddObserver(new TracingObserver());
    }
}
