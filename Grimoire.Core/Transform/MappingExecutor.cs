using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Grimoire.Core.Extract;
using Grimoire.Core.Results;

namespace Grimoire.Core.Transform;

internal sealed class MappingExecutor<TEntity> where TEntity : class, new()
{
    private readonly List<PropertyMapping> _mappings;
    private readonly KeyMap.KeyMap _keyMap;
    private readonly string _entityName;
    private static readonly ConcurrentDictionary<MemberInfo, Action<TEntity, object?>> _setterCache = new();

    public MappingExecutor(List<PropertyMapping> mappings, KeyMap.KeyMap keyMap, string entityName)
    {
        _mappings = mappings;
        _keyMap = keyMap;
        _entityName = entityName;
    }

    public (TEntity? Entity, RowError? Error) Transform(SourceRow row)
    {
        var entity = new TEntity();

        foreach (var mapping in _mappings)
        {
            try
            {
                var value = row[mapping.SourceColumn];

                if (value is null or DBNull && mapping.HasDefault)
                    value = mapping.DefaultValue;

                if (mapping.ForeignKeyEntityType is not null)
                {
                    if (value is null or DBNull)
                    {
                        value = null;
                    }
                    else
                    {
                        var resolved = _keyMap.Resolve(mapping.ForeignKeyEntityType, value);
                        if (resolved is null)
                        {
                            return (null, new RowError(
                                _entityName,
                                RowErrorType.ForeignKeyNotFound,
                                $"Foreign key not found for {mapping.ForeignKeyEntityType.Name} with legacy key '{value}'",
                                row.ToDictionary()));
                        }
                        value = resolved;
                    }
                }

                if (mapping.Converter is not null)
                    value = mapping.Converter(value);

                var setter = GetOrCompileSetter(mapping.TargetProperty);
                var targetType = GetMemberType(mapping.TargetProperty);
                var converted = ConvertValue(value, targetType);
                setter(entity, converted);
            }
            catch (Exception ex)
            {
                return (null, new RowError(
                    _entityName,
                    RowErrorType.MappingError,
                    $"Failed to map column '{mapping.SourceColumn}' to '{mapping.TargetProperty.Name}': {ex.Message}",
                    row.ToDictionary(),
                    ex));
            }
        }

        return (entity, null);
    }

    private static Action<TEntity, object?> GetOrCompileSetter(MemberInfo member)
    {
        return _setterCache.GetOrAdd(member, CompileSetter);
    }

    private static Action<TEntity, object?> CompileSetter(MemberInfo member)
    {
        var entityParam = Expression.Parameter(typeof(TEntity), "entity");
        var valueParam = Expression.Parameter(typeof(object), "value");
        var memberType = GetMemberType(member);
        var castValue = Expression.Convert(valueParam, memberType);
        var memberAccess = Expression.MakeMemberAccess(entityParam, member);
        var assign = Expression.Assign(memberAccess, castValue);
        return Expression.Lambda<Action<TEntity, object?>>(assign, entityParam, valueParam).Compile();
    }

    private static Type GetMemberType(MemberInfo member) => member switch
    {
        PropertyInfo prop => prop.PropertyType,
        FieldInfo field => field.FieldType,
        _ => throw new InvalidOperationException($"Unsupported member type: {member.MemberType}")
    };

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null or DBNull)
        {
            return targetType.IsValueType
                ? (Nullable.GetUnderlyingType(targetType) is not null ? null : Activator.CreateInstance(targetType))
                : null;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsInstanceOfType(value))
            return value;

        if (underlying.IsEnum)
            return Enum.ToObject(underlying, value);

        return System.Convert.ChangeType(value, underlying);
    }
}
