namespace Grimoire.Core.Transform;

public abstract class GrimoireMapping<TEntity> where TEntity : class, new()
{
    public abstract void Configure(IMappingBuilder<TEntity> builder);

    internal MappingBuilder<TEntity> Build()
    {
        var builder = new MappingBuilder<TEntity>();
        Configure(builder);
        return builder;
    }
}
