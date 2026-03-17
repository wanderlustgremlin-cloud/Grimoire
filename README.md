# Grimoire

A .NET ETL engine shipped as a single NuGet package. Grimoire handles transform, load, and orchestration — callers own their source and target connections.

## How It Works

An external app (typically via a scheduled job) provides:

1. **A connector** — implements `IConnector` (DB sources) or `ICustomExtractor` (APIs, files, queues)
2. **Mapping config** — fluent `GrimoireMapping<T>` classes with full type safety and intellisense
3. **Load config** — target connection string, table, and batch size

Grimoire streams data from the source, applies mappings (including FK resolution via an in-memory key map), and bulk loads into the target database. Results (success/failure, row counts, errors) are returned to the caller.

```csharp
var result = await new GrimoirePipeline()
    .ExtractFrom(new OracleErpConnector(connectionString))
    .Entity<PurchaseOrder>(po =>
    {
        po.TransformUsing<PurchaseOrderMapping>();
        po.LoadInto(new LoadConfig
        {
            ConnectionString = warehouseConnection,
            TargetTable = "dbo.DimPurchaseOrder",
            BatchSize = 5000
        });
        po.TrackKey(o => o.Id, "LEGACY_PO_ID");
        po.MatchOn(o => o.PoNumber)
           .WhenMatched(UpdateStrategy.OverwriteAll);
    })
    .Entity<PurchaseOrderLine>(line =>
    {
        line.TransformUsing<PurchaseOrderLineMapping>();
        line.LoadInto(lineLoadConfig);
        line.DependsOn<PurchaseOrder>();
        line.MatchOn(o => o.PoNumber, o => o.LineNumber);
    })
    .OnRowError(error => logger.LogWarning("Row error: {Error}", error))
    .ExecuteAsync(cancellationToken);
```

## Packages

| Package | Description |
|---------|-------------|
| **Grimoire.Core** | The ETL engine — extraction interfaces, fluent mapping, key map, bulk load, pipeline orchestration |

### Observability (optional)

| Package | Description |
|---------|-------------|
| **Grimoire.Observability.ActivityLog** | Activity logging |
| **Grimoire.Observability.Logging** | Logging abstractions |
| **Grimoire.Observability.Message** | Messaging/notifications |
| **Grimoire.Observability.Metrics** | Metrics collection |
| **Grimoire.Observability.OpenTelemetry** | OpenTelemetry integration |
| **Grimoire.Observability.SignalR** | SignalR real-time progress |

## Key Features

- **Single package** — install `Grimoire.Core`, nothing else required
- **Caller-owned connections** — caller provides both source and target. `IConnector` for databases, `ICustomExtractor` for anything else
- **Connector schema** — connector authors define table relationships, mapping authors just reference table names
- **Fluent mapping API** — type-safe `GrimoireMapping<T>` with intellisense on target entity properties
- **Key map FK resolution** — in-memory `{LegacyKey → NewKey}` per pipeline run, O(1) lookups
- **Upsert via MatchOn** — business key duplicate detection with configurable update strategies
- **Multi-entity pipelines** — `DependsOn<T>()` with automatic topological sort
- **Row-level error handling** — bad rows are skipped and reported, not fatal
- **Streaming** — `IAsyncEnumerable` throughout, natural backpressure
- **Event-based observability** — optional hooks for progress, errors, and completion

## Getting Started

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build Grimoire.slnx
```

## License

[MIT](LICENSE)
