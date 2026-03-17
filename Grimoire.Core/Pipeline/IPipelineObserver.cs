using Grimoire.Core.Results;

namespace Grimoire.Core.Pipeline;

public interface IPipelineObserver
{
    void OnPipelineStarted() { }
    void OnEntityStarted(string entityName) { }
    void OnProgress(string entityName, int rowsProcessed) { }
    void OnRowError(RowError error) { }
    void OnEntityComplete(EntityResult result) { }
    void OnPipelineComplete(EtlResult result) { }
}
