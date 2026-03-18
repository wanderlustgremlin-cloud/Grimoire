namespace Grimoire.Core.Load;

public sealed class LoadConfig
{
    public ITargetProvider? Provider { get; set; }
    public required string TargetTable { get; init; }
    public int BatchSize { get; init; } = 1000;
}
