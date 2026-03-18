using System.Reflection;

namespace Grimoire.Core.Load;

internal sealed class UpsertHandler
{
    private readonly MatchConfig _matchConfig;
    private readonly ITargetSession _session;
    private readonly List<PropertyInfo> _properties;
    private readonly HashSet<string> _generatedColumns;

    public UpsertHandler(MatchConfig matchConfig, ITargetSession session, List<PropertyInfo> properties, HashSet<string> generatedColumns)
    {
        _matchConfig = matchConfig;
        _session = session;
        _properties = properties;
        _generatedColumns = generatedColumns;
    }

    public async Task<(int Inserted, int Updated, int Skipped)> UpsertBatchAsync<TEntity>(
        List<TEntity> batch,
        CancellationToken cancellationToken) where TEntity : class
    {
        var matchProps = _properties
            .Where(p => _matchConfig.MatchColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var nonMatchProps = _properties
            .Where(p => !_matchConfig.MatchColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var insertProps = _properties
            .Where(p => !_generatedColumns.Contains(p.Name))
            .ToList();

        var updateProps = nonMatchProps
            .Where(p => !_generatedColumns.Contains(p.Name))
            .ToList();

        var generatedProp = _properties
            .FirstOrDefault(p => _generatedColumns.Contains(p.Name));

        int inserted = 0, updated = 0, skipped = 0;

        foreach (var entity in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matchValues = matchProps
                .Select(p => (p.Name, p.GetValue(entity)))
                .ToList();

            var selectColumns = nonMatchProps
                .Select(p => p.Name)
                .ToList();

            var existingRow = await _session.FindRowAsync(matchValues, selectColumns, cancellationToken);

            if (existingRow is null)
            {
                var insertValues = insertProps
                    .Select(p => (p.Name, p.GetValue(entity)))
                    .ToList();

                var generatedValue = await _session.InsertRowAsync(insertValues, generatedProp?.Name, cancellationToken);

                if (generatedProp is not null && generatedValue is not null)
                    generatedProp.SetValue(entity, Convert.ChangeType(generatedValue, generatedProp.PropertyType));

                inserted++;
            }
            else
            {
                if (generatedProp is not null && existingRow.TryGetValue(generatedProp.Name, out var existingId) && existingId is not null)
                    generatedProp.SetValue(entity, Convert.ChangeType(existingId, generatedProp.PropertyType));

                if (_matchConfig.WhenMatchedStrategy == UpdateStrategy.Skip)
                {
                    skipped++;
                }
                else if (_matchConfig.WhenMatchedStrategy == UpdateStrategy.OverwriteChanged)
                {
                    if (HasChanges(entity, existingRow, updateProps))
                    {
                        var setValues = updateProps
                            .Select(p => (p.Name, p.GetValue(entity)))
                            .ToList();

                        var whereValues = matchProps
                            .Select(p => (p.Name, p.GetValue(entity)))
                            .ToList();

                        await _session.UpdateRowAsync(setValues, whereValues, cancellationToken);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else // OverwriteAll
                {
                    var setValues = updateProps
                        .Select(p => (p.Name, p.GetValue(entity)))
                        .ToList();

                    var whereValues = matchProps
                        .Select(p => (p.Name, p.GetValue(entity)))
                        .ToList();

                    await _session.UpdateRowAsync(setValues, whereValues, cancellationToken);
                    updated++;
                }
            }
        }

        return (inserted, updated, skipped);
    }

    private static bool HasChanges<TEntity>(TEntity entity, Dictionary<string, object?> existingRow, List<PropertyInfo> props) where TEntity : class
    {
        foreach (var prop in props)
        {
            var newValue = prop.GetValue(entity);
            existingRow.TryGetValue(prop.Name, out var oldValue);

            if (newValue is null && oldValue is null) continue;
            if (newValue is null || oldValue is null) return true;
            if (!newValue.Equals(oldValue)) return true;
        }
        return false;
    }
}
