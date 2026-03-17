using System.Reflection;

namespace Grimoire.Core.Transform;

public sealed class PropertyMapping
{
    public required MemberInfo TargetProperty { get; init; }
    public required string SourceColumn { get; init; }
    public Type? ForeignKeyEntityType { get; set; }
    public Func<object?, object?>? Converter { get; set; }
    public object? DefaultValue { get; set; }
    public bool HasDefault { get; set; }
}
