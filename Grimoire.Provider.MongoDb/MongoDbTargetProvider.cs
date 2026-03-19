using Grimoire.Core.Load;
using MongoDB.Driver;

namespace Grimoire.Provider.MongoDb;

public sealed class MongoDbTargetProvider : ITargetProvider
{
    private readonly IMongoClient _client;
    private readonly string _databaseName;

    public MongoDbTargetProvider(string connectionString, string databaseName)
    {
        _client = new MongoClient(connectionString);
        _databaseName = databaseName;
    }

    public async Task<ITargetSession> BeginSessionAsync(string targetTable, CancellationToken ct)
    {
        var database = _client.GetDatabase(_databaseName);
        var collection = database.GetCollection<MongoDB.Bson.BsonDocument>(targetTable);
        var session = await _client.StartSessionAsync(cancellationToken: ct);
        session.StartTransaction();
        return new MongoDbTargetSession(session, collection);
    }
}
