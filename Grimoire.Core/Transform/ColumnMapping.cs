namespace Grimoire.Core.Transform;

public sealed class ColumnMapping<TEntity> where TEntity : class, new()
{
    internal PropertyMapping Mapping { get; }

    internal ColumnMapping(PropertyMapping mapping)
    {
        Mapping = mapping;
    }

    public ColumnMapping<TEntity> AsForeignKey<TForeignEntity>()
    {
        Mapping.ForeignKeyEntityType = typeof(TForeignEntity);
        return this;
    }

    public ColumnMapping<TEntity> Convert(Func<object?, object?> converter)
    {
        Mapping.Converter = converter;
        return this;
    }

    public ColumnMapping<TEntity> Default(object? defaultValue)
    {
        Mapping.DefaultValue = defaultValue;
        Mapping.HasDefault = true;
        return this;
    }
}
