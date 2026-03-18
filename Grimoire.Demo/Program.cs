using Grimoire.Demo;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerClient("legacy", configureSettings: settings => settings.DisableTracing = true);
builder.AddSqlServerClient("target", configureSettings: settings => settings.DisableTracing = true);

builder.Services.AddHostedService<EtlWorker>();

var host = builder.Build();
host.Run();
