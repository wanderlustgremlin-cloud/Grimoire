namespace Grimoire.Core.Extract;

public sealed class SourceRow
{
    private readonly Dictionary<string, object?> _data = new(StringComparer.OrdinalIgnoreCase);

    public object? this[string column]
    {
        get => _data.TryGetValue(column, out var value) ? value : null;
        set => _data[column] = value;
    }

    public bool ContainsColumn(string column) => _data.ContainsKey(column);

    public IEnumerable<string> Columns => _data.Keys;

    public IDictionary<string, object?> ToDictionary() => new Dictionary<string, object?>(_data, StringComparer.OrdinalIgnoreCase);

    public static SourceRow FromDictionary(IDictionary<string, object?> data)
    {
        var row = new SourceRow();
        foreach (var kvp in data)
            row[kvp.Key] = kvp.Value;
        return row;
    }
}
