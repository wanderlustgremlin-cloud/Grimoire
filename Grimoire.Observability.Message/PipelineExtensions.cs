using Grimoire.Core.Pipeline;

namespace Grimoire.Observability.Message;

public static class PipelineExtensions
{
    public static GrimoirePipeline AddMessaging(this GrimoirePipeline pipeline, IMessageSender sender, string pipelineName = "Grimoire ETL")
    {
        return pipeline.AddObserver(new MessageObserver(sender, pipelineName));
    }
}
