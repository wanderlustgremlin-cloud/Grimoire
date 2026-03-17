using Grimoire.Core.Pipeline;

namespace Grimoire.Observability.ActivityLog;

public static class PipelineExtensions
{
    public static GrimoirePipeline AddActivityLog(this GrimoirePipeline pipeline, string connectionString)
    {
        return pipeline.AddObserver(new ActivityLogObserver(connectionString));
    }
}
