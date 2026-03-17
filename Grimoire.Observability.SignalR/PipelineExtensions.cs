using Grimoire.Core.Pipeline;
using Microsoft.AspNetCore.SignalR;

namespace Grimoire.Observability.SignalR;

public static class PipelineExtensions
{
    public static GrimoirePipeline AddSignalR(this GrimoirePipeline pipeline, IHubContext<GrimoireHub, IGrimoireHub> hubContext)
    {
        return pipeline.AddObserver(new SignalRObserver(hubContext));
    }
}
