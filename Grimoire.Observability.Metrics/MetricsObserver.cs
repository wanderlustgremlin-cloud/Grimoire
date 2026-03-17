using System.Diagnostics;
using System.Diagnostics.Metrics;
using Grimoire.Core.Pipeline;
using Grimoire.Core.Results;

namespace Grimoire.Observability.Metrics;

public sealed class MetricsObserver : IPipelineObserver
{
    private readonly Counter<long> _rowsInserted;
    private readonly Counter<long> _rowsUpdated;
    private readonly Counter<long> _rowsErrored;
    private readonly Histogram<double> _pipelineDuration;
    private readonly Histogram<double> _entityDuration;
    private readonly Histogram<double> _batchDuration;
    private readonly Histogram<double> _batchRowsPerSec;

    private readonly Dictionary<string, Stopwatch> _entityTimers = [];
    private Stopwatch? _pipelineTimer;

    public MetricsObserver(IMeterFactory? meterFactory = null)
    {
        var meter = meterFactory?.Create("Grimoire.ETL") ?? new Meter("Grimoire.ETL");

        _rowsInserted = meter.CreateCounter<long>("grimoire.rows.inserted", "rows", "Number of rows inserted");
        _rowsUpdated = meter.CreateCounter<long>("grimoire.rows.updated", "rows", "Number of rows updated");
        _rowsErrored = meter.CreateCounter<long>("grimoire.rows.errored", "rows", "Number of rows with errors");
        _pipelineDuration = meter.CreateHistogram<double>("grimoire.pipeline.duration", "ms", "Pipeline execution duration");
        _entityDuration = meter.CreateHistogram<double>("grimoire.entity.duration", "ms", "Entity execution duration");
        _batchDuration = meter.CreateHistogram<double>("grimoire.batch.duration", "ms", "Batch load duration");
        _batchRowsPerSec = meter.CreateHistogram<double>("grimoire.batch.rows_per_sec", "rows/s", "Batch load throughput");
    }

    public void OnPipelineStarted()
    {
        _pipelineTimer = Stopwatch.StartNew();
    }

    public void OnEntityStarted(string entityName)
    {
        _entityTimers[entityName] = Stopwatch.StartNew();
    }

    public void OnRowError(RowError error)
    {
        _rowsErrored.Add(1, new KeyValuePair<string, object?>("entity", error.EntityName));
    }

    public void OnBatchLoaded(BatchResult batch)
    {
        var entityTag = new KeyValuePair<string, object?>("entity", batch.EntityName);
        var batchSizeTag = new KeyValuePair<string, object?>("batch_size", batch.BatchSize);
        _batchDuration.Record(batch.Duration.TotalMilliseconds, entityTag, batchSizeTag);

        if (batch.Duration.TotalSeconds > 0)
            _batchRowsPerSec.Record(batch.RowsInBatch / batch.Duration.TotalSeconds, entityTag, batchSizeTag);
    }

    public void OnEntityComplete(EntityResult result)
    {
        if (_entityTimers.Remove(result.EntityName, out var timer))
        {
            timer.Stop();
            _entityDuration.Record(timer.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("entity", result.EntityName));
        }

        _rowsInserted.Add(result.RowsInserted, new KeyValuePair<string, object?>("entity", result.EntityName));
        _rowsUpdated.Add(result.RowsUpdated, new KeyValuePair<string, object?>("entity", result.EntityName));
    }

    public void OnPipelineComplete(EtlResult result)
    {
        _pipelineTimer?.Stop();
        if (_pipelineTimer is not null)
            _pipelineDuration.Record(_pipelineTimer.Elapsed.TotalMilliseconds);
    }
}
