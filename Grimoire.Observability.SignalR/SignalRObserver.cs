using Grimoire.Core.Pipeline;
using Grimoire.Core.Results;
using Microsoft.AspNetCore.SignalR;

namespace Grimoire.Observability.SignalR;

public sealed class SignalRObserver : IPipelineObserver
{
    private readonly IHubContext<GrimoireHub, IGrimoireHub> _hubContext;

    public SignalRObserver(IHubContext<GrimoireHub, IGrimoireHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public void OnPipelineStarted()
    {
        _hubContext.Clients.All.PipelineStarted().GetAwaiter().GetResult();
    }

    public void OnProgress(string entityName, int rowsProcessed)
    {
        _hubContext.Clients.All.ProgressUpdated(entityName, rowsProcessed).GetAwaiter().GetResult();
    }

    public void OnRowError(RowError error)
    {
        _hubContext.Clients.All.ErrorOccurred(new ErrorArgs
        {
            EntityName = error.EntityName,
            ErrorType = error.ErrorType.ToString(),
            Message = error.Message
        }).GetAwaiter().GetResult();
    }

    public void OnEntityComplete(EntityResult result)
    {
        _hubContext.Clients.All.EntityCompleted(result.EntityName, new EntityCompletedArgs
        {
            RowsInserted = result.RowsInserted,
            RowsUpdated = result.RowsUpdated,
            RowsErrored = result.RowsErrored,
            Success = result.Success,
            Duration = result.Duration
        }).GetAwaiter().GetResult();
    }

    public void OnPipelineComplete(EtlResult result)
    {
        _hubContext.Clients.All.PipelineCompleted(new PipelineCompletedArgs
        {
            Success = result.Success,
            TotalRowsInserted = result.TotalRowsInserted,
            TotalRowsUpdated = result.TotalRowsUpdated,
            TotalRowsErrored = result.TotalRowsErrored,
            TotalDuration = result.TotalDuration
        }).GetAwaiter().GetResult();
    }
}
