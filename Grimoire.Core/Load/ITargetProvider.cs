namespace Grimoire.Core.Load;

public interface ITargetProvider
{
    Task<ITargetSession> BeginSessionAsync(string targetTable, CancellationToken ct);
}
