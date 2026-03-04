# Grimoire

A collection of .NET NuGet libraries that provide config-driven ETL (Extract, Transform, Load) logic and observability.

## ETL Projects

Grimoire provides ETL functionality as composable NuGet packages. Add only what you need:

| Package | Description | Dependencies |
|---------|-------------|--------------|
| **Grimoire.Extract** | Data extraction | None |
| **Grimoire.Transform** | Default transform logic | Extract |
| **Grimoire.Load** | Data loading | Extract, Transform |

## Observability Projects

| Package | Description |
|---------|-------------|
| **Grimoire.Observability.ActivityLog** | Activity logging |
| **Grimoire.Observability.Logging** | Logging abstractions |
| **Grimoire.Observability.Message** | Messaging/notifications |
| **Grimoire.Observability.Metrics** | Metrics collection |
| **Grimoire.Observability.OpenTelemetry** | OpenTelemetry integration |
| **Grimoire.Observability.SignalR** | SignalR integration for observability |

## Getting Started

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build Grimoire.slnx
```

## License

[MIT](LICENSE)
