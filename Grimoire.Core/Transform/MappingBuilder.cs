using System.Linq.Expressions;
using System.Reflection;

namespace Grimoire.Core.Transform;

public sealed class MappingBuilder<TEntity> : IMappingBuilder<TEntity> where TEntity : class, new()
{
    internal List<PropertyMapping> Mappings { get; } = [];
    internal List<string> SourceTables { get; } = [];

    public ColumnMapping<TEntity> Map(Expression<Func<TEntity, object?>> targetProperty, string sourceColumn)
    {
        var member = ExtractMember(targetProperty);
        var mapping = new PropertyMapping
        {
            TargetProperty = member,
            SourceColumn = sourceColumn
        };
        Mappings.Add(mapping);
        return new ColumnMapping<TEntity>(mapping);
    }

    public IMappingBuilder<TEntity> FromTables(params string[] tables)
    {
        SourceTables.AddRange(tables);
        return this;
    }

    private static MemberInfo ExtractMember(Expression<Func<TEntity, object?>> expression)
    {
        var body = expression.Body;

        // Unwrap Convert nodes (boxing value types to object)
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
            body = unary.Operand;

        if (body is MemberExpression memberExpr)
            return memberExpr.Member;

        throw new ArgumentException($"Expression must be a simple property access, got: {expression}");
    }
}
