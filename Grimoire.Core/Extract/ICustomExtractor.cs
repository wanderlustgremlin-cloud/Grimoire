namespace Grimoire.Core.Extract;

public interface ICustomExtractor
{
    IAsyncEnumerable<SourceRow> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default);
}
