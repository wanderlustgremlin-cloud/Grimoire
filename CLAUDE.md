# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Grimoire is a .NET ETL engine. `Grimoire.Core` defines the pipeline, mappings, and load interfaces. Provider packages (e.g., `Grimoire.Provider.SqlServer`) implement database-specific load operations. External apps provide connectors and mapping configs; Grimoire handles transform, load, and orchestration. Licensed under MIT.

## Build Commands

```bash
# Build entire solution
dotnet build Grimoire.slnx

# Build a single project
dotnet build Grimoire.Core/Grimoire.Core.csproj

# Run tests
dotnet test Grimoire.Core.Tests/Grimoire.Core.Tests.csproj

# Run the Aspire demo (requires Docker)
dotnet run --project Grimoire.Demo.AppHost
```

## Architecture

Multi-project solution using .slnx format (XML-based solution file). All projects target **net10.0** with nullable reference types and implicit usings enabled.

**Core ETL Engine:**

- **Grimoire.Core** — The ETL engine: extraction interfaces (`IConnector`, `ICustomExtractor`), fluent mapping API (`GrimoireMapping<T>`), key map FK resolution, upsert via `MatchOn`, load abstraction (`ITargetProvider`/`ITargetSession`), multi-entity pipeline orchestration with `DependsOn<T>()` topological sort, `IPipelineObserver` for lifecycle events, row-level error handling, and `EtlResult` reporting. **No database driver dependency** — all DB-specific logic lives in provider packages.

**Provider Packages (one per target database):**

- **Grimoire.Provider.SqlServer** — SQL Server target: `SqlBulkCopy` for bulk insert, `OUTPUT INSERTED` for generated key readback, `sys.columns` for identity detection, `[bracket]` quoting. Depends on `Microsoft.Data.SqlClient`.
- **Grimoire.Provider.Postgres** — PostgreSQL target: multi-row `INSERT` for bulk insert, `RETURNING` for generated key readback, `information_schema.columns` for identity/serial detection, `"double quote"` quoting. Depends on `Npgsql`.
- **Grimoire.Provider.Oracle** — Oracle target: `INSERT ALL ... SELECT FROM DUAL` for bulk insert (sub-batched at 1000 rows), `RETURNING INTO` with output parameter for generated key readback, `USER_TAB_COLUMNS` for identity detection, `"double quote"` quoting. Depends on `Oracle.ManagedDataAccess.Core`.
- **Grimoire.Provider.MongoDb** — MongoDB target: `InsertManyAsync` for bulk insert, auto-generated `_id` (maps entity `Id` property to `_id`), BSON filter/update builders for find/update, session transactions (requires replica set). Depends on `MongoDB.Driver`.

**Observability Packages (optional, observer-based via `IPipelineObserver`):**

- **Grimoire.Observability.Logging** — Structured logging via `ILoggerFactory` (`pipeline.AddLogging()`)
- **Grimoire.Observability.Metrics** — Counters and histograms via `System.Diagnostics.Metrics` (`pipeline.AddMetrics()`)
- **Grimoire.Observability.OpenTelemetry** — Distributed tracing with `ActivitySource("Grimoire.ETL")` (`pipeline.AddTracing()`)
- **Grimoire.Observability.ActivityLog** — Persists run history to `[GrimoireActivityLog]` SQL table (`pipeline.AddActivityLog()`). Has its own `Microsoft.Data.SqlClient` dependency (independent of provider packages).
- **Grimoire.Observability.Message** — Caller-defined notifications via `IMessageSender` (`pipeline.AddMessaging()`)
- **Grimoire.Observability.SignalR** — Real-time progress via `GrimoireHub` (`pipeline.AddSignalR()`)

**Prebuilt Connectors (source-side, implement `IConnector`):**

- **Grimoire.Connector.OracleEbs** — Oracle E-Business Suite connector (`IConnector`) with version-aware schema registry. Supports EBS versions R11i, R12, R12.1, R12.2. Modules: HR (people, assignments, orgs, jobs, positions, grades, locations), AP (suppliers, invoices, distributions, payments), AR (customers/TCA, transactions, receipts), GL (journals, balances, code combinations, ledgers), PO (purchase orders, requisitions, distributions), INV (items, categories, on-hand, transactions). Schema mode: `Apps` (APPS schema views, default) or `BaseTables` (direct base table access). Version-range declarations on columns/joins ensure only valid schema elements are included. Depends on `Oracle.ManagedDataAccess.Core`.
- **Grimoire.Connector.BusinessCentral** — Microsoft Dynamics 365 Business Central extractor (`ICustomExtractor`) for OData API. Supports API v1.0 and v2.0 with version-aware field filtering. Modules: Sales (customers, orders, invoices, credit memos, quotes), Purchasing (vendors, POs, invoices), Financials (GL entries, accounts, dimensions, journals, currencies, tax groups/areas, payment terms/methods), Inventory (items, categories, variants, UoMs, locations), HR (employees). Handles OData pagination via `@odata.nextLink`. Caller provides `HttpClient` with auth configured (OAuth2). Zero external dependencies beyond Grimoire.Core.

**Test Project:**

- **Grimoire.Core.Tests** — 103 xUnit tests covering KeyMap, SourceRow, SchemaBuilder, MappingBuilder, MappingExecutor, TopologicalSorter, EntityBuilder, pipeline validation (including target provider validation), observer pattern, SQL dialects (quoting, schema qualification, pagination for all 4 providers), and result types. Uses `InternalsVisibleTo` for internal type access.

**Demo Project (Aspire):**

- **Grimoire.Demo.AppHost** — Aspire orchestrator: provisions SQL Server container with `legacy` and `target` databases
- **Grimoire.Demo.ServiceDefaults** — Standard Aspire service defaults with `Grimoire.ETL` meter and activity source registered
- **Grimoire.Demo** — Worker service that seeds data and runs a 3-entity pipeline (Department -> Employee -> Responsibility) demonstrating connectors, `SqlServerTargetProvider`, mappings, FK resolution, upserts, converters, and observability

## Key Design Decisions

- **Core + Provider separation** — `Grimoire.Core` has zero database driver dependencies. All DB-specific logic (bulk insert, identity detection, SQL generation, identifier quoting) lives in provider packages that implement `ITargetProvider`/`ITargetSession`. Callers install Core + one or more provider packages.
- **ITargetProvider / ITargetSession** — `ITargetProvider` is a factory that creates `ITargetSession` instances (one per entity load). `ITargetSession` encapsulates connection, transaction, and all DB operations (bulk insert, find, insert, update, generated column detection, key readback, commit/rollback). Uses `IReadOnlyList<IReadOnlyDictionary<string, object?>>` for bulk rows instead of `DataTable` — works for both relational and document DBs.
- **Pipeline-level and per-entity targets** — `LoadWith(provider)` sets a default target for all entities. Individual entities can override with `LoadInto(table, provider, batchSize)`. Validation ensures every entity has a provider before execution.
- **SQL dialect abstraction** — `ISqlDialect` interface with `QuoteIdentifier`, `QualifyTable`, and `BuildPagination`. Built-in dialects for SqlServer (`[brackets]`, `OFFSET/FETCH`), Postgres (`"double quotes"`, `LIMIT/OFFSET`), Oracle (`"double quotes"`, `FETCH FIRST`), and MySQL (`` `backticks` ``, `LIMIT/OFFSET`). Auto-resolved from `IConnector.Provider` via `DialectFactory`, or overridden via `IConnector.Dialect`. `ConnectorExtractor` uses the dialect for all SQL generation. Schema qualification supported via `Table("name", schema: "legacy")` in `ConfigureSchema`.
- **Caller-owned connections** — Grimoire does not own source or target databases. The caller provides a connector for extraction and a target provider for loading.
- **Fluent mapping** — `GrimoireMapping<T>` with expression-based property mappings for type safety and intellisense. Source columns are strings (external DB), target properties use `e => e.Property`.
- **Key map** — runtime `{EntityType, LegacyKey} → NewKey` dictionary scoped to pipeline run. `TrackKey` registers after load, `AsForeignKey<T>()` resolves in mappings. `TrackKeyLegacyColumn` is automatically included in extraction source columns.
- **Upsert** — `MatchOn` declares business keys for duplicate detection. Strategies: `OverwriteAll`, `OverwriteChanged`, `Skip`. NULL match values use `IS NULL` semantics (not `= NULL`).
- **Identity column handling** — Provider sessions detect identity/generated columns and exclude them from INSERT/UPDATE, reading back generated values so key tracking works with DB-generated IDs.
- **Multi-entity pipelines** — `DependsOn<T>()` declares execution order, Grimoire topologically sorts.
- **Per-entity transactions** — each entity commits independently via `ITargetSession.CommitAsync`.
- **Row-level errors** — bad rows skipped and collected in `EtlResult`, not fatal.
- **No retry** — Grimoire reports results, caller owns retry logic.
- **Streaming** — `IAsyncEnumerable<SourceRow>` throughout, configurable `BatchSize` in `LoadConfig`.
- **Observer pattern** — `IPipelineObserver` interface with default no-op methods. Multiple observers can be attached simultaneously. Lifecycle events: `OnPipelineStarted`, `OnEntityStarted`, `OnProgress`, `OnRowError`, `OnBatchLoaded`, `OnEntityComplete`, `OnPipelineComplete`. Legacy single-delegate callbacks (`OnRowError()`, `OnProgress()`, `OnEntityComplete()`) also supported.

The solution file (`Grimoire.slnx`) references projects as they are developed. New projects need to be added to it.
