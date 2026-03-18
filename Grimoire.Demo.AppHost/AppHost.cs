var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var legacyDb = sqlServer.AddDatabase("legacy");
var targetDb = sqlServer.AddDatabase("target");

builder.AddProject<Projects.Grimoire_Demo>("grimoire-demo")
    .WithReference(legacyDb).WaitFor(legacyDb)
    .WithReference(targetDb).WaitFor(targetDb);

builder.Build().Run();
