using Grimoire.Core.Pipeline;
using Grimoire.Core.Results;

namespace Grimoire.Observability.Message;

public sealed class MessageObserver : IPipelineObserver
{
    private readonly IMessageSender _sender;
    private readonly string _pipelineName;

    public MessageObserver(IMessageSender sender, string pipelineName = "Grimoire ETL")
    {
        _sender = sender;
        _pipelineName = pipelineName;
    }

    public void OnPipelineComplete(EtlResult result)
    {
        var message = new PipelineMessage
        {
            PipelineName = _pipelineName,
            Success = result.Success,
            TotalDuration = result.TotalDuration,
            EntitySummaries = result.EntityResults.Select(r => new EntitySummary
            {
                EntityName = r.EntityName,
                RowsInserted = r.RowsInserted,
                RowsUpdated = r.RowsUpdated,
                RowsErrored = r.RowsErrored,
                Duration = r.Duration,
                Success = r.Success
            }).ToList(),
            Errors = result.EntityResults
                .SelectMany(r => r.Errors)
                .Select(e => $"[{e.EntityName}] {e.ErrorType}: {e.Message}")
                .ToList()
        };

        // Fire-and-forget since IPipelineObserver methods are synchronous.
        // The caller's IMessageSender should handle its own error reporting.
        _ = Task.Run(async () => await _sender.SendAsync(message));
    }
}
