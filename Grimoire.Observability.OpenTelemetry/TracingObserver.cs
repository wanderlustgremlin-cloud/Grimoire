using System.Diagnostics;
using Grimoire.Core.Pipeline;
using Grimoire.Core.Results;

namespace Grimoire.Observability.OpenTelemetry;

public sealed class TracingObserver : IPipelineObserver
{
    private static readonly ActivitySource Source = new("Grimoire.ETL");

    private Activity? _pipelineActivity;
    private readonly Dictionary<string, Activity?> _entityActivities = [];

    public void OnPipelineStarted()
    {
        _pipelineActivity = Source.StartActivity("grimoire.pipeline");
    }

    public void OnEntityStarted(string entityName)
    {
        var activity = Source.StartActivity("grimoire.entity");
        activity?.SetTag("entity.name", entityName);
        _entityActivities[entityName] = activity;
    }

    public void OnProgress(string entityName, int rowsProcessed)
    {
        if (_entityActivities.TryGetValue(entityName, out var activity))
            activity?.SetTag("rows.processed", rowsProcessed);
    }

    public void OnRowError(RowError error)
    {
        if (_entityActivities.TryGetValue(error.EntityName, out var activity))
        {
            activity?.AddEvent(new ActivityEvent("row.error", tags: new ActivityTagsCollection
            {
                { "error.type", error.ErrorType.ToString() },
                { "error.message", error.Message }
            }));
        }
    }

    public void OnBatchLoaded(BatchResult batch)
    {
        if (_entityActivities.TryGetValue(batch.EntityName, out var activity))
        {
            var rowsPerSec = batch.Duration.TotalSeconds > 0
                ? batch.RowsInBatch / batch.Duration.TotalSeconds
                : 0;

            activity?.AddEvent(new ActivityEvent("batch.loaded", tags: new ActivityTagsCollection
            {
                { "batch.number", batch.BatchNumber },
                { "batch.rows", batch.RowsInBatch },
                { "batch.duration_ms", batch.Duration.TotalMilliseconds },
                { "batch.rows_per_sec", rowsPerSec },
                { "batch.size", batch.BatchSize },
                { "batch.inserted", batch.RowsInserted },
                { "batch.updated", batch.RowsUpdated }
            }));
        }
    }

    public void OnEntityComplete(EntityResult result)
    {
        if (_entityActivities.Remove(result.EntityName, out var activity) && activity is not null)
        {
            activity.SetTag("rows.inserted", result.RowsInserted);
            activity.SetTag("rows.updated", result.RowsUpdated);
            activity.SetTag("rows.errored", result.RowsErrored);
            activity.SetTag("entity.success", result.Success);
            activity.Dispose();
        }
    }

    public void OnPipelineComplete(EtlResult result)
    {
        if (_pipelineActivity is not null)
        {
            _pipelineActivity.SetTag("pipeline.success", result.Success);
            _pipelineActivity.SetTag("rows.inserted.total", result.TotalRowsInserted);
            _pipelineActivity.SetTag("rows.updated.total", result.TotalRowsUpdated);
            _pipelineActivity.SetTag("rows.errored.total", result.TotalRowsErrored);
            _pipelineActivity.Dispose();
            _pipelineActivity = null;
        }
    }
}
