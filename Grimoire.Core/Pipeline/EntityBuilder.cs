using Grimoire.Core.Extract;
using Grimoire.Core.Load;
using Grimoire.Core.Transform;

namespace Grimoire.Core.Pipeline;

public sealed class EntityBuilder<TEntity> where TEntity : class, new()
{
    internal EntityRegistration Registration { get; }
    private readonly GrimoirePipeline _pipeline;

    internal EntityBuilder(GrimoirePipeline pipeline)
    {
        _pipeline = pipeline;
        Registration = new EntityRegistration
        {
            EntityType = typeof(TEntity),
            EntityName = typeof(TEntity).Name
        };
    }

    public EntityBuilder<TEntity> TransformUsing<TMapping>() where TMapping : GrimoireMapping<TEntity>, new()
    {
        Registration.Mapping = new TMapping();
        return this;
    }

    public EntityBuilder<TEntity> TransformUsing(GrimoireMapping<TEntity> mapping)
    {
        Registration.Mapping = mapping;
        return this;
    }

    public EntityBuilder<TEntity> ExtractUsing(ICustomExtractor extractor)
    {
        Registration.CustomExtractor = extractor;
        return this;
    }

    public EntityBuilder<TEntity> LoadInto(string targetTable, string connectionString, int batchSize = 1000)
    {
        Registration.LoadConfig = new LoadConfig
        {
            TargetTable = targetTable,
            ConnectionString = connectionString,
            BatchSize = batchSize
        };
        return this;
    }

    public EntityBuilder<TEntity> LoadInto(LoadConfig config)
    {
        Registration.LoadConfig = config;
        return this;
    }

    public EntityBuilder<TEntity> DependsOn<TDependency>() where TDependency : class
    {
        Registration.Dependencies.Add(typeof(TDependency));
        return this;
    }

    public EntityBuilder<TEntity> TrackKey(string targetKeyProperty, string legacyKeyColumn)
    {
        Registration.TrackKeyProperty = targetKeyProperty;
        Registration.TrackKeyLegacyColumn = legacyKeyColumn;
        return this;
    }

    public EntityBuilder<TEntity> MatchOn(Action<MatchOnBuilder> configure)
    {
        var matchBuilder = new MatchOnBuilder();
        configure(matchBuilder);
        Registration.MatchConfig = matchBuilder.Build();
        return this;
    }

    public GrimoirePipeline Done() => _pipeline;
}

public sealed class MatchOnBuilder
{
    private readonly MatchConfig _config = new();

    public MatchOnBuilder Columns(params string[] columns)
    {
        _config.MatchColumns.AddRange(columns);
        return this;
    }

    public MatchOnBuilder WhenMatched(UpdateStrategy strategy)
    {
        _config.WhenMatchedStrategy = strategy;
        return this;
    }

    internal MatchConfig Build() => _config;
}
