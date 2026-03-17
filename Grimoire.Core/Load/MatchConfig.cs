namespace Grimoire.Core.Load;

public sealed class MatchConfig
{
    public List<string> MatchColumns { get; } = [];
    public UpdateStrategy WhenMatchedStrategy { get; set; } = UpdateStrategy.OverwriteAll;
}
