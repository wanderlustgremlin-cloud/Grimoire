using Grimoire.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace Grimoire.Observability.Logging;

public static class PipelineExtensions
{
    public static GrimoirePipeline AddLogging(this GrimoirePipeline pipeline, ILoggerFactory loggerFactory)
    {
        return pipeline.AddObserver(new LoggingObserver(loggerFactory));
    }
}
