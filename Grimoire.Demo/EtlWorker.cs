using Grimoire.Core.Load;
using Grimoire.Core.Pipeline;
using Grimoire.Demo.Entities;
using Grimoire.Demo.Mappings;
using Grimoire.Observability.Logging;
using Grimoire.Observability.Metrics;
using Grimoire.Observability.OpenTelemetry;

namespace Grimoire.Demo;

public class EtlWorker(
    ILogger<EtlWorker> logger,
    ILoggerFactory loggerFactory,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give Aspire a moment to provision the databases
        await Task.Delay(2000, stoppingToken);

        var legacyConnStr = configuration.GetConnectionString("legacy")
            ?? throw new InvalidOperationException("Missing 'legacy' connection string");
        var targetConnStr = configuration.GetConnectionString("target")
            ?? throw new InvalidOperationException("Missing 'target' connection string");

        try
        {
            // 1. Seed the legacy database and prepare target schema
            logger.LogInformation("Seeding legacy database...");
            await DatabaseSeeder.SeedLegacyDatabaseAsync(legacyConnStr, stoppingToken);

            logger.LogInformation("Preparing target database schema...");
            await DatabaseSeeder.PrepareTargetDatabaseAsync(targetConnStr, stoppingToken);

            // 2. Build and run the Grimoire ETL pipeline
            logger.LogInformation("Starting ETL pipeline...");

            var result = await new GrimoirePipeline()
                // Source database connector
                .ExtractFrom(new LegacyConnector(legacyConnStr))

                // Observability — visible in Aspire dashboard
                .AddLogging(loggerFactory)
                .AddMetrics()
                .AddTracing()

                // Entity: Departments (extracted first, no dependencies)
                .Entity<Department>()
                    .TransformUsing<DepartmentMapping>()
                    .LoadInto("Departments", targetConnStr, batchSize: 100)
                    .MatchOn(m => m.Columns("Name").WhenMatched(UpdateStrategy.Skip))
                    .TrackKey("Id", "DeptName")
                    .Done()

                // Entity: Employees (depends on Departments for FK resolution)
                .Entity<Employee>()
                    .TransformUsing<EmployeeMapping>()
                    .LoadInto("Employees", targetConnStr, batchSize: 500)
                    .MatchOn(m => m.Columns("Email").WhenMatched(UpdateStrategy.OverwriteChanged))
                    .DependsOn<Department>()
                    .TrackKey("Id", "EmpId")
                    .Done()

                // Entity: Responsibilities (depends on Employees for FK resolution)
                .Entity<Responsibility>()
                    .TransformUsing<ResponsibilityMapping>()
                    .LoadInto("Responsibilities", targetConnStr, batchSize: 500)
                    .DependsOn<Employee>()
                    .Done()

                .ExecuteAsync(stoppingToken);

            // 3. Report results
            logger.LogInformation("=== ETL Complete ===");
            logger.LogInformation("Success: {Success}", result.Success);
            logger.LogInformation("Total Duration: {Duration}", result.TotalDuration);
            logger.LogInformation("Total Inserted: {Inserted}", result.TotalRowsInserted);
            logger.LogInformation("Total Updated: {Updated}", result.TotalRowsUpdated);
            logger.LogInformation("Total Errors: {Errors}", result.TotalRowsErrored);

            foreach (var entity in result.EntityResults)
            {
                logger.LogInformation(
                    "  {Entity}: Extracted={Extracted}, Inserted={Inserted}, Updated={Updated}, Errors={Errors}, Duration={Duration}",
                    entity.EntityName, entity.RowsExtracted, entity.RowsInserted,
                    entity.RowsUpdated, entity.RowsErrored, entity.Duration);
            }

            // Verify target data
            await VerifyTargetDataAsync(targetConnStr, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ETL pipeline failed");
        }

        // Stop the host after ETL completes
        lifetime.StopApplication();
    }

    private async Task VerifyTargetDataAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM Departments";
        var deptCount = (int)(await cmd.ExecuteScalarAsync(cancellationToken))!;

        cmd.CommandText = "SELECT COUNT(*) FROM Employees";
        var empCount = (int)(await cmd.ExecuteScalarAsync(cancellationToken))!;

        cmd.CommandText = "SELECT COUNT(*) FROM Responsibilities";
        var respCount = (int)(await cmd.ExecuteScalarAsync(cancellationToken))!;

        logger.LogInformation("=== Target Database Verification ===");
        logger.LogInformation("Departments: {Count}", deptCount);
        logger.LogInformation("Employees: {Count}", empCount);
        logger.LogInformation("Responsibilities: {Count}", respCount);

        cmd.CommandText = """
            SELECT e.FirstName, e.LastName, d.Name AS Department, r.Title
            FROM Employees e
            JOIN Departments d ON e.DepartmentId = d.Id
            LEFT JOIN Responsibilities r ON r.EmployeeId = e.Id
            ORDER BY d.Name, e.LastName, r.Title
            """;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var title = reader.IsDBNull(reader.GetOrdinal("Title")) ? "(none)" : reader["Title"];
            logger.LogInformation("  {FirstName} {LastName} — {Department} — {Title}",
                reader["FirstName"], reader["LastName"], reader["Department"], title);
        }
    }
}
