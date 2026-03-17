using Grimoire.Core.Pipeline;
using Grimoire.Core.Results;
using Microsoft.Extensions.Logging;

namespace Grimoire.Observability.Logging;

public sealed class LoggingObserver : IPipelineObserver
{
    private readonly ILogger _logger;

    public LoggingObserver(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("Grimoire.ETL");
    }

    public void OnPipelineStarted()
    {
        _logger.LogInformation("Pipeline started");
    }

    public void OnEntityStarted(string entityName)
    {
        _logger.LogInformation("Entity {EntityName} started", entityName);
    }

    public void OnProgress(string entityName, int rowsProcessed)
    {
        _logger.LogInformation("Entity {EntityName}: {RowsProcessed} rows processed", entityName, rowsProcessed);
    }

    public void OnRowError(RowError error)
    {
        _logger.LogWarning(error.Exception, "Row error in {EntityName}: [{ErrorType}] {Message}",
            error.EntityName, error.ErrorType, error.Message);
    }

    public void OnBatchLoaded(BatchResult batch)
    {
        var rowsPerSec = batch.Duration.TotalSeconds > 0
            ? batch.RowsInBatch / batch.Duration.TotalSeconds
            : 0;

        _logger.LogInformation(
            "Entity {EntityName} batch {BatchNumber}: {RowsInBatch} rows in {Duration:N0}ms ({RowsPerSec:N0} rows/sec) — BatchSize: {BatchSize}",
            batch.EntityName, batch.BatchNumber, batch.RowsInBatch,
            batch.Duration.TotalMilliseconds, rowsPerSec, batch.BatchSize);
    }

    public void OnEntityComplete(EntityResult result)
    {
        _logger.LogInformation(
            "Entity {EntityName} complete — Inserted: {Inserted}, Updated: {Updated}, Errors: {Errors}, Duration: {Duration}",
            result.EntityName, result.RowsInserted, result.RowsUpdated, result.RowsErrored, result.Duration);
    }

    public void OnPipelineComplete(EtlResult result)
    {
        var level = result.Success ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(level,
            "Pipeline complete — Success: {Success}, Inserted: {Inserted}, Updated: {Updated}, Errors: {Errors}, Duration: {Duration}",
            result.Success, result.TotalRowsInserted, result.TotalRowsUpdated, result.TotalRowsErrored, result.TotalDuration);
    }
}
