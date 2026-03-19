# Grimoire

A .NET ETL engine shipped as a NuGet package. Grimoire handles transform, load, and orchestration — callers own their source and target connections.

## How It Works

An external app provides:

1. **A connector** — implements `IConnector` (DB sources with schema declaration) or `ICustomExtractor` (APIs, files, queues)
2. **Mapping config** — fluent `GrimoireMapping<T>` classes with type-safe property expressions
3. **A target provider** — implements `ITargetProvider` (e.g., `SqlServerTargetProvider`) to handle database-specific load operations

Grimoire streams data from the source, applies mappings (including FK resolution via an in-memory key map), and loads into the target database through the provider abstraction. Results are returned to the caller.

## Quick Example

```csharp
var target = new SqlServerTargetProvider(targetConnectionString);

var result = await new GrimoirePipeline()
    .ExtractFrom(new LegacyConnector(sourceConnectionString))
    .LoadWith(target)
    .AddLogging(loggerFactory)
    .AddMetrics()
    .AddTracing()

    .Entity<Department>()
        .TransformUsing<DepartmentMapping>()
        .LoadInto("Departments", batchSize: 100)
        .MatchOn(m => m.Columns("Name").WhenMatched(UpdateStrategy.Skip))
        .TrackKey("Id", "DeptName")
        .Done()

    .Entity<Employee>()
        .TransformUsing<EmployeeMapping>()
        .LoadInto("Employees", batchSize: 500)
        .MatchOn(m => m.Columns("Email").WhenMatched(UpdateStrategy.OverwriteChanged))
        .DependsOn<Department>()
        .TrackKey("Id", "EmpId")
        .Done()

    .Entity<Responsibility>()
        .TransformUsing<ResponsibilityMapping>()
        .LoadInto("Responsibilities", batchSize: 500)
        .MatchOn(m => m.Columns("EmployeeId", "Title").WhenMatched(UpdateStrategy.OverwriteChanged))
        .DependsOn<Employee>()
        .Done()

    .ExecuteAsync(cancellationToken);
```

## Target Provider

The target provider abstracts away database-specific load operations. Grimoire.Core defines two interfaces — `ITargetProvider` (factory) and `ITargetSession` (operations) — and provider packages implement them.

```csharp
// Pipeline-level target (common case: all entities go to same DB)
var target = new SqlServerTargetProvider(targetConnStr);
pipeline.LoadWith(target);

// Per-entity override (e.g., one entity goes to a different DB)
pipeline.Entity<AuditLog>()
    .TransformUsing<AuditMapping>()
    .LoadInto("audit_logs", differentProvider)
    .Done();
```

**Available providers:**

| Package | Target | Bulk Strategy |
|---------|--------|---------------|
| **Grimoire.Provider.SqlServer** | SQL Server | `SqlBulkCopy`, `OUTPUT INSERTED`, `sys.columns` |
| **Grimoire.Provider.Postgres** | PostgreSQL | Multi-row `INSERT` with `RETURNING`, `information_schema` |
| **Grimoire.Provider.Oracle** | Oracle | `INSERT ALL`, `RETURNING INTO`, `USER_TAB_COLUMNS` |
| **Grimoire.Provider.MongoDb** | MongoDB | `InsertManyAsync`, `_id` auto-generation, filter builders |

## Connector

The connector describes the source database schema. Grimoire generates SQL queries automatically based on the schema and each mapping's `FromTables()` declaration. An `ISqlDialect` handles identifier quoting, schema qualification, and pagination for each database.

```csharp
public class LegacyConnector(string connectionString) : IConnector
{
    public DatabaseProvider Provider => DatabaseProvider.SqlServer;
    public DbConnection CreateConnection() => new SqlConnection(connectionString);

    public void ConfigureSchema(ISchemaBuilder schema)
    {
        schema
            .Table("LegacyEmployees")
                .Columns("EmpId", "FullName", "Email", "DeptName", "HireDate", "IsActive")
                .JoinTo("LegacyResponsibilities", "EmpId", "EmpId")
                .Done()
            .Table("LegacyResponsibilities")
                .Columns("ResponsibilityId", "EmpId", "Responsibility", "AssignedDate")
                .Done();
    }
}
```

### SQL Dialects

Grimoire auto-selects a dialect from `IConnector.Provider`. Built-in dialects handle quoting, schema prefixes, and pagination:

| Dialect | Quoting | Schema Example | Pagination |
|---------|---------|----------------|------------|
| **SQL Server** | `[identifier]` | `[dbo].[Table]` | `OFFSET x ROWS FETCH NEXT y ROWS ONLY` |
| **PostgreSQL** | `"identifier"` | `"public"."table"` | `LIMIT y OFFSET x` |
| **Oracle** | `"identifier"` | `"SCHEMA"."TABLE"` | `FETCH FIRST y ROWS ONLY` |
| **MySQL** | `` `identifier` `` | `` `db`.`table` `` | `LIMIT y OFFSET x` |

Schema qualification is optional — use `Table("name", schema: "legacy")` in `ConfigureSchema`. To override the default dialect, implement `ISqlDialect` and return it from `IConnector.Dialect`.

For non-database sources (APIs, files, queues), implement `ICustomExtractor` instead and use `.ExtractUsing()` per entity.

## Mappings

Each entity gets a `GrimoireMapping<T>` that maps source columns to target properties. Converters, defaults, and FK resolution are all fluent.

```csharp
public class EmployeeMapping : GrimoireMapping<Employee>
{
    public override void Configure(IMappingBuilder<Employee> builder)
    {
        builder.FromTables("LegacyEmployees");

        builder.Map(e => e.FirstName, "FullName")
            .Convert(v => v?.ToString()?.Split(' ', 2)[0]);

        builder.Map(e => e.LastName, "FullName")
            .Convert(v => { var parts = v?.ToString()?.Split(' ', 2); return parts?.Length > 1 ? parts[1] : ""; });

        builder.Map(e => e.Email, "Email");

        builder.Map(e => e.DepartmentId, "DeptName")
            .AsForeignKey<Department>();  // Resolves via KeyMap

        builder.Map(e => e.HireDate, "HireDate");

        builder.Map(e => e.IsActive, "IsActive")
            .Default(true);  // Used when source value is NULL
    }
}
```

## Pipeline Features

| Feature | Description |
|---------|-------------|
| **`ExtractFrom(connector)`** | Set the source database connector for all entities |
| **`LoadWith(provider)`** | Set the default target provider for all entities |
| **`Entity<T>()`** | Register an entity for ETL processing |
| **`TransformUsing<TMapping>()`** | Apply a mapping class to transform source rows |
| **`LoadInto(table, batchSize)`** | Configure target table and batch size (uses pipeline default provider) |
| **`LoadInto(table, provider, batchSize)`** | Configure target table with a per-entity provider override |
| **`MatchOn(m => m.Columns(...))`** | Upsert: declare business keys for duplicate detection |
| **`WhenMatched(strategy)`** | `OverwriteAll`, `OverwriteChanged`, or `Skip` |
| **`DependsOn<T>()`** | Declare execution order; Grimoire topologically sorts |
| **`TrackKey(targetProp, legacyCol)`** | Register identity mappings for FK resolution by downstream entities |
| **`AsForeignKey<T>()`** | Resolve a source value through the key map to a target FK |
| **`AddObserver(observer)`** | Attach an `IPipelineObserver` for lifecycle events |
| **`OnRowError` / `OnProgress`** | Simple callback hooks for events |

## Observability

All observability is opt-in via `IPipelineObserver`. Each package provides a fluent `pipeline.AddX()` extension. Lifecycle events include pipeline start/end, entity start/complete, batch loaded (with timing and throughput), row errors, and progress updates.

| Package | Extension | What it does |
|---------|-----------|-------------|
| **Grimoire.Observability.Logging** | `.AddLogging(loggerFactory)` | Structured logging via `Microsoft.Extensions.Logging` |
| **Grimoire.Observability.Metrics** | `.AddMetrics(meterFactory?)` | Counters and histograms via `System.Diagnostics.Metrics` |
| **Grimoire.Observability.OpenTelemetry** | `.AddTracing()` | Distributed tracing with `ActivitySource("Grimoire.ETL")` |
| **Grimoire.Observability.ActivityLog** | `.AddActivityLog(connStr)` | Persists run history to a `[GrimoireActivityLog]` SQL table |
| **Grimoire.Observability.Message** | `.AddMessaging(sender)` | Caller-defined notifications via `IMessageSender` |
| **Grimoire.Observability.SignalR** | `.AddSignalR(hubContext)` | Real-time progress via SignalR hub |

### Batch Performance Metrics

Each batch load reports timing data through observers, enabling users to tune `BatchSize` for optimal throughput:

```
Entity Employee batch 1: 500 rows in 89ms (5618 rows/sec) — BatchSize: 500
Entity Employee batch 2: 500 rows in 72ms (6944 rows/sec) — BatchSize: 500
```

## Demo Project

The `Grimoire.Demo` project is a complete working example using .NET Aspire for local orchestration. It migrates a flat `LegacyEmployees` table (with related `LegacyResponsibilities`) into normalized `Departments`, `Employees`, and `Responsibilities` tables.

**What it demonstrates:**

- `IConnector` implementation with `ConfigureSchema` (multi-table with joins)
- `SqlServerTargetProvider` with pipeline-level `LoadWith()` for target database
- 3-tier dependency chain: Department -> Employee -> Responsibility
- `TrackKey` + `AsForeignKey` for FK resolution across entities
- `MatchOn` with different upsert strategies (`Skip`, `OverwriteChanged`)
- Converters (splitting `FullName` into `FirstName`/`LastName`)
- Defaults (`IsActive` defaults to `true` when source is NULL)
- Logging, Metrics, and OpenTelemetry observers visible in the Aspire dashboard

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) and Docker.

```bash
dotnet run --project Grimoire.Demo.AppHost
```

Aspire spins up a SQL Server container, creates source and target databases, seeds test data, and runs the ETL pipeline. Open the Aspire dashboard URL printed to the console to see structured logs, traces, and metrics.

## Prebuilt Connectors

Grimoire provides prebuilt connectors for common source systems so you don't have to implement `IConnector` yourself.

### Oracle E-Business Suite

`Grimoire.Connector.OracleEbs` provides a version-aware connector for Oracle EBS with 6 modules (HR, AP, AR, GL, PO, INV). Tables, columns, and joins are filtered to your EBS version — no need to know which tables exist in R11i vs R12.2.

```csharp
var source = new OracleEbsConnector(
    connectionString,
    EbsVersion.R122,
    modules =>
    {
        modules.HR();    // all HR tables
        modules.AP();    // all AP tables
        modules.GL();    // all GL tables
    },
    schemaMode: EbsSchemaMode.Apps   // default: APPS schema views
);

var result = await new GrimoirePipeline()
    .ExtractFrom(source)
    .LoadWith(new SqlServerTargetProvider(targetConnStr))
    // ... entity mappings ...
    .ExecuteAsync(ct);
```

Cherry-pick specific tables within a module:

```csharp
var source = new OracleEbsConnector(oracleConnStr, EbsVersion.R121, modules =>
{
    modules.HR(hr => hr.People().Organizations().Assignments());
    modules.AP(ap => ap.Invoices().Suppliers());
});
```

**Schema mode:** `EbsSchemaMode.Apps` (default) uses the supported APPS schema views. `EbsSchemaMode.BaseTables` queries base tables directly for performance.

**Supported versions:** `R11i`, `R12`, `R121`, `R122` — columns and joins that don't exist in your version are automatically excluded.

## Building

```bash
dotnet build Grimoire.slnx
dotnet test
```

## License

[MIT](LICENSE)
