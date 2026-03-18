namespace Grimoire.Core.Load;

public interface ITargetSession : IAsyncDisposable
{
    Task<HashSet<string>> GetGeneratedColumnsAsync(CancellationToken ct);

    Task BulkInsertAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<string> columns,
        int batchSize,
        CancellationToken ct);

    Task<Dictionary<string, object?>?> FindRowAsync(
        IReadOnlyList<(string Column, object? Value)> matchValues,
        IReadOnlyList<string> selectColumns,
        CancellationToken ct);

    Task<object?> InsertRowAsync(
        IReadOnlyList<(string Column, object? Value)> values,
        string? generatedColumn,
        CancellationToken ct);

    Task UpdateRowAsync(
        IReadOnlyList<(string Column, object? Value)> setValues,
        IReadOnlyList<(string Column, object? Value)> whereValues,
        CancellationToken ct);

    Task<object?> ReadGeneratedKeyAsync(
        string keyColumn,
        IReadOnlyList<(string Column, object? Value)> matchValues,
        CancellationToken ct);

    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
