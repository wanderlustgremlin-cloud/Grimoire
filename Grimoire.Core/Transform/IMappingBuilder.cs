using System.Linq.Expressions;

namespace Grimoire.Core.Transform;

public interface IMappingBuilder<TEntity> where TEntity : class, new()
{
    ColumnMapping<TEntity> Map(Expression<Func<TEntity, object?>> targetProperty, string sourceColumn);
    IMappingBuilder<TEntity> FromTables(params string[] tables);
}
