using Grimoire.Core.Load;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Grimoire.Provider.MongoDb;

public sealed class MongoDbTargetSession : ITargetSession
{
    private readonly IClientSessionHandle _session;
    private readonly IMongoCollection<BsonDocument> _collection;

    internal MongoDbTargetSession(IClientSessionHandle session, IMongoCollection<BsonDocument> collection)
    {
        _session = session;
        _collection = collection;
    }

    public Task<HashSet<string>> GetGeneratedColumnsAsync(CancellationToken ct)
    {
        // MongoDB auto-generates _id; map entity "Id" property to "_id"
        return Task.FromResult(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Id" });
    }

    public async Task BulkInsertAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<string> columns,
        int batchSize,
        CancellationToken ct)
    {
        if (rows.Count == 0) return;

        var documents = new List<BsonDocument>(rows.Count);
        foreach (var row in rows)
        {
            var doc = new BsonDocument();
            foreach (var col in columns)
            {
                if (row.TryGetValue(col, out var value))
                {
                    doc[MapFieldName(col)] = BsonValue.Create(value);
                }
            }
            documents.Add(doc);
        }

        await _collection.InsertManyAsync(_session, documents, cancellationToken: ct);
    }

    public async Task<Dictionary<string, object?>?> FindRowAsync(
        IReadOnlyList<(string Column, object? Value)> matchValues,
        IReadOnlyList<string> selectColumns,
        CancellationToken ct)
    {
        var filter = BuildFilter(matchValues);
        var projection = Builders<BsonDocument>.Projection.Include(MapFieldName(selectColumns[0]));
        for (int i = 1; i < selectColumns.Count; i++)
        {
            projection = projection.Include(MapFieldName(selectColumns[i]));
        }

        var doc = await _collection.Find(_session, filter).Project(projection).FirstOrDefaultAsync(ct);
        if (doc is null) return null;

        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in doc)
        {
            var fieldName = UnmapFieldName(element.Name);
            row[fieldName] = BsonValueToObject(element.Value);
        }
        return row;
    }

    public async Task<object?> InsertRowAsync(
        IReadOnlyList<(string Column, object? Value)> values,
        string? generatedColumn,
        CancellationToken ct)
    {
        var doc = new BsonDocument();
        foreach (var (column, value) in values)
        {
            doc[MapFieldName(column)] = BsonValue.Create(value);
        }

        await _collection.InsertOneAsync(_session, doc, cancellationToken: ct);

        if (generatedColumn is not null)
        {
            var mappedField = MapFieldName(generatedColumn);
            if (doc.Contains(mappedField))
            {
                return BsonValueToObject(doc[mappedField]);
            }
        }

        return null;
    }

    public async Task UpdateRowAsync(
        IReadOnlyList<(string Column, object? Value)> setValues,
        IReadOnlyList<(string Column, object? Value)> whereValues,
        CancellationToken ct)
    {
        var filter = BuildFilter(whereValues);
        var updateDefs = setValues.Select(v =>
            Builders<BsonDocument>.Update.Set(MapFieldName(v.Column), BsonValue.Create(v.Value)));
        var update = Builders<BsonDocument>.Update.Combine(updateDefs);

        await _collection.UpdateOneAsync(_session, filter, update, cancellationToken: ct);
    }

    public async Task<object?> ReadGeneratedKeyAsync(
        string keyColumn,
        IReadOnlyList<(string Column, object? Value)> matchValues,
        CancellationToken ct)
    {
        var filter = BuildFilter(matchValues);
        var mappedKey = MapFieldName(keyColumn);
        var projection = Builders<BsonDocument>.Projection.Include(mappedKey);

        var doc = await _collection.Find(_session, filter).Project(projection).FirstOrDefaultAsync(ct);
        if (doc is null) return null;

        return doc.Contains(mappedKey) ? BsonValueToObject(doc[mappedKey]) : null;
    }

    public async Task CommitAsync(CancellationToken ct)
    {
        await _session.CommitTransactionAsync(ct);
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        await _session.AbortTransactionAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        _session.Dispose();
        return ValueTask.CompletedTask;
    }

    private static FilterDefinition<BsonDocument> BuildFilter(IReadOnlyList<(string Column, object? Value)> matchValues)
    {
        var filters = matchValues.Select(m =>
        {
            var field = MapFieldName(m.Column);
            return m.Value is null
                ? Builders<BsonDocument>.Filter.Eq(field, BsonNull.Value)
                : Builders<BsonDocument>.Filter.Eq(field, BsonValue.Create(m.Value));
        });
        return Builders<BsonDocument>.Filter.And(filters);
    }

    private static string MapFieldName(string entityField)
        => entityField.Equals("Id", StringComparison.OrdinalIgnoreCase) ? "_id" : entityField;

    private static string UnmapFieldName(string mongoField)
        => mongoField == "_id" ? "Id" : mongoField;

    private static object? BsonValueToObject(BsonValue value)
    {
        if (value.IsBsonNull) return null;
        return value.BsonType switch
        {
            BsonType.ObjectId => value.AsObjectId.ToString(),
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => value.AsDouble,
            BsonType.String => value.AsString,
            BsonType.Boolean => value.AsBoolean,
            BsonType.DateTime => value.ToUniversalTime(),
            BsonType.Decimal128 => value.AsDecimal,
            _ => value.ToString()
        };
    }
}
