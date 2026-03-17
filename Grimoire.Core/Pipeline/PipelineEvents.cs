using Grimoire.Core.Results;

namespace Grimoire.Core.Pipeline;

public sealed class PipelineEvents
{
    public Action<RowError>? OnRowError { get; set; }
    public Action<string, int>? OnProgress { get; set; }
    public Action<EntityResult>? OnEntityComplete { get; set; }
}
