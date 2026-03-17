using System.Diagnostics.Metrics;
using Grimoire.Core.Pipeline;

namespace Grimoire.Observability.Metrics;

public static class PipelineExtensions
{
    public static GrimoirePipeline AddMetrics(this GrimoirePipeline pipeline, IMeterFactory? meterFactory = null)
    {
        return pipeline.AddObserver(new MetricsObserver(meterFactory));
    }
}
