namespace Grimoire.Core.Load;

public sealed class LoadConfig
{
    public required string ConnectionString { get; init; }
    public required string TargetTable { get; init; }
    public int BatchSize { get; init; } = 1000;
}
